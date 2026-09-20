namespace SimmerAndSear.Application.Commerce;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2025. Records [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record> [Accessed 18 September 2026].
//
//Microsoft, 2026. C# classes [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/classes> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────


public sealed record CartOwner
{
    private CartOwner(string? clientId, string? sessionId)
    {
        ClientId = clientId;
        SessionId = sessionId;
    }

    public string? ClientId { get; }

    public string? SessionId { get; }

    public bool IsAnonymous => ClientId is null;

    public static CartOwner ForClient(string clientId) =>
        string.IsNullOrWhiteSpace(clientId)
            ? throw new ArgumentException("A client cart needs a client id.", nameof(clientId))
            : new CartOwner(clientId, null);

    public static CartOwner ForSession(string sessionId) =>
        string.IsNullOrWhiteSpace(sessionId)
            ? throw new ArgumentException("An anonymous cart needs a session id.", nameof(sessionId))
            : new CartOwner(null, sessionId);
}
