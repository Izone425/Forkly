namespace Forkly.Api.Dtos;

// ---- Admin user management (all under /api/admin/*, admin-only) ----

// Row shape for the admin "Users" table. Lighter than UserDto — no addresses —
// but adds the admin-only flags the management screen needs.
public class AdminUserListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();

    // Convenience flags derived from Roles / lockout state for the UI toggles.
    public bool IsAdmin { get; set; }
    public bool IsDisabled { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

// Full detail for a single user (the base profile plus admin-only fields).
public class AdminUserDetailDto
{
    public UserDto User { get; set; } = new();
    public bool IsAdmin { get; set; }
    public bool IsDisabled { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

// Generic paged envelope reused by the users list (and mirrored in OrderService).
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = new List<T>();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
