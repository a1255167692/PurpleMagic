using System.Text.Json.Serialization;

namespace ServerS5A3;

// Explicit metadata keeps the shared wire contract usable by the native AOT host.
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(PlayerProfile))]
[JsonSerializable(typeof(PlayerPreferences))]
[JsonSerializable(typeof(LoginReply))]
[JsonSerializable(typeof(AccountRequest))]
[JsonSerializable(typeof(RedeemRequest))]
[JsonSerializable(typeof(CharacterQuery))]
[JsonSerializable(typeof(CharacterEntry))]
[JsonSerializable(typeof(CharacterEntry[]))]
[JsonSerializable(typeof(CharacterListReply))]
[JsonSerializable(typeof(BatchRedeemRequest))]
[JsonSerializable(typeof(BatchRedeemReply))]
[JsonSerializable(typeof(SignedEnvelope))]
[JsonSerializable(typeof(UpdateSettings))]
[JsonSerializable(typeof(ReleaseManifest))]
public partial class CoreJson : JsonSerializerContext { }

public sealed record AccountRequest(string account, string password, string device);
public sealed record RedeemRequest(string account, string token, string code, string? characterId);
public sealed record CharacterQuery(string account, string token);
public sealed record CharacterEntry(string id, string name, int level, string job);
public sealed record CharacterListReply(bool Ok, CharacterEntry[]? Characters, string? Message);
public sealed record BatchRedeemRequest(string account, string token, string? characterId, string[] codes);
public sealed record BatchRedeemItem(string code, bool ok, string message);
public sealed record BatchRedeemReply(bool Ok, string Message, BatchRedeemItem[]? results, int succeeded, int failed);
