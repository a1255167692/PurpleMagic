using System.Net.Http.Json;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace ServerS5A3;

public sealed record LoginReply(bool Ok, string? Account_id, string? Token, string? Message);

public sealed class PlayerApi : IDisposable
{
    public HttpClient Http { get; }
    public PlayerProfile Profile { get; }
    private readonly Action<int>? responseObserved;
    public static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, TypeInfoResolver = CoreJson.Default };
    public PlayerApi(PlayerProfile profile, Action<int>? responseObserved = null)
    {
        Profile = profile;
        this.responseObserved = responseObserved;
        Http = new HttpClient(new HttpClientHandler {
            UseProxy = false, AllowAutoRedirect = false,
            ServerCertificateCustomValidationCallback = (request, cert, chain, errors) =>
                CertificateAllowed(request.RequestUri!, cert, errors, profile)
        }) { Timeout = TimeSpan.FromSeconds(20) };
    }
    public static bool CertificateAllowed(Uri uri, X509Certificate2? cert, SslPolicyErrors errors, PlayerProfile profile)
    {
        if (cert == null || uri.Scheme != "https") return false;
        var origin = new Uri(profile.ApiUrl);
        if (uri.Host != origin.Host || uri.Port != origin.Port) return errors == SslPolicyErrors.None;
        return DateTime.UtcNow >= cert.NotBefore.ToUniversalTime() && DateTime.UtcNow <= cert.NotAfter.ToUniversalTime()
            && (errors & SslPolicyErrors.RemoteCertificateNameMismatch) == 0
            && Convert.ToHexString(SHA256.HashData(cert.RawData)).Equals(profile.CertificateSha256, StringComparison.OrdinalIgnoreCase);
    }
    public async Task<LoginReply> Submit(string action, string account, string password, string device)
    {
        if (action is not ("register" or "login")) throw new ArgumentException(nameof(action));
        using var response = await Http.PostAsJsonAsync(Profile.ApiUrl + "/api/account/" + action, new AccountRequest(account, password, device), CoreJson.Default.AccountRequest);
        try { responseObserved?.Invoke((int)response.StatusCode); } catch (Exception) { }
        var reply = await response.Content.ReadFromJsonAsync(CoreJson.Default.LoginReply);
        if (reply?.Ok != true) throw new InvalidOperationException(reply?.Message ?? "账号服务暂不可用。");
        return reply;
    }
    public async Task CheckStatus()
    {
        using var response = await Http.GetAsync(Profile.ApiUrl + "/api/status");
        response.EnsureSuccessStatusCode();
    }
    // 卡密兑换：用登录时拿到的令牌证明账号归属，服务端校验后发奖。
    public async Task<LoginReply> Redeem(string account, string token, string code, string? characterId = null)
    {
        using var response = await Http.PostAsJsonAsync(Profile.ApiUrl + "/api/account/redeem",
            new RedeemRequest(account, token, code, string.IsNullOrWhiteSpace(characterId) ? null : characterId), CoreJson.Default.RedeemRequest);
        try { responseObserved?.Invoke((int)response.StatusCode); } catch (Exception) { }
        var reply = await response.Content.ReadFromJsonAsync(CoreJson.Default.LoginReply);
        if (reply?.Ok != true) throw new InvalidOperationException(reply?.Message ?? "兑换服务暂不可用。");
        return reply;
    }
    // 拉取账号下的角色列表（兑换时选投递目标）。
    public async Task<CharacterListReply> Characters(string account, string token)
    {
        using var response = await Http.PostAsJsonAsync(Profile.ApiUrl + "/api/account/characters",
            new CharacterQuery(account, token), CoreJson.Default.CharacterQuery);
        response.EnsureSuccessStatusCode();
        var reply = await response.Content.ReadFromJsonAsync(CoreJson.Default.CharacterListReply);
        if (reply?.Ok != true) throw new InvalidOperationException(reply?.Message ?? "读取角色列表失败。");
        return reply;
    }

    // 批量兑换：一次提交多张卡密，逐张返回结果。
    public async Task<BatchRedeemReply> RedeemBatch(string account, string token, string? characterId, string[] codes)
    {
        using var response = await Http.PostAsJsonAsync(Profile.ApiUrl + "/api/account/redeem-batch",
            new BatchRedeemRequest(account, token, string.IsNullOrWhiteSpace(characterId) ? null : characterId, codes),
            CoreJson.Default.BatchRedeemRequest);
        response.EnsureSuccessStatusCode();
        var reply = await response.Content.ReadFromJsonAsync(CoreJson.Default.BatchRedeemReply);
        if (reply == null) throw new InvalidOperationException("兑换服务返回为空。");
        return reply;
    }

    public void Dispose() => Http.Dispose();
}
