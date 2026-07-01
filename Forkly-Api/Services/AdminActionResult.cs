using Forkly.Api.Dtos;

namespace Forkly.Api.Services;

// Transport-neutral outcome of an admin mutation (promote/demote, enable/disable).
// The controller maps Error to an HTTP status; success carries the updated row so
// the admin UI can refresh without a second request.
public enum AdminActionError
{
    None = 0,
    NotFound,
    LastAdmin,   // refused: would remove the only remaining admin
    SelfAction,  // refused: admin tried to demote/disable their own account
}

public class AdminActionResult
{
    public bool Succeeded { get; private init; }
    public AdminActionError Error { get; private init; }
    public string? Message { get; private init; }
    public AdminUserListItemDto? User { get; private init; }

    public static AdminActionResult Ok(AdminUserListItemDto user) =>
        new() { Succeeded = true, User = user };

    public static AdminActionResult Fail(AdminActionError error, string message) =>
        new() { Succeeded = false, Error = error, Message = message };
}
