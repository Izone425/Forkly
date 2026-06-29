using Forkly.Api.Dtos;

namespace Forkly.Api.Services;

// Transport-neutral outcome of an account operation. The REST controller maps
// this to HTTP status codes and the gRPC service maps it to gRPC status codes,
// so neither transport carries business logic.
public enum AccountErrorKind
{
    None = 0,
    Conflict,       // e.g. email already registered
    Unauthorized,   // bad credentials
    Validation,     // input rejected (weak password, etc.)
    NotFound,
}

public class AccountResult
{
    public bool Succeeded { get; private init; }
    public AuthResponse? Auth { get; private init; }
    public AccountErrorKind ErrorKind { get; private init; }
    public IReadOnlyList<string> Errors { get; private init; } = Array.Empty<string>();

    public static AccountResult Success(AuthResponse? auth = null) =>
        new() { Succeeded = true, Auth = auth };

    public static AccountResult Fail(AccountErrorKind kind, params string[] errors) =>
        new() { Succeeded = false, ErrorKind = kind, Errors = errors };
}
