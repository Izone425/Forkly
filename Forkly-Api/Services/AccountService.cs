using Forkly.Api.Data;
using Forkly.Api.Dtos;
using Forkly.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Forkly.Api.Services;

// All real account logic lives here. Identity handles password hashing and
// validation; we layer role assignment and JWT issuance on top.
public class AccountService : IAccountService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly TokenService _tokenService;
    private readonly AppDbContext _db;

    public AccountService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        TokenService tokenService,
        AppDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _db = db;
    }

    public async Task<AccountResult> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim();

        if (await _userManager.FindByEmailAsync(email) is not null)
            return AccountResult.Fail(AccountErrorKind.Conflict, "Email is already registered.");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = request.FullName.Trim(),
        };

        var created = await _userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
            return AccountResult.Fail(AccountErrorKind.Validation,
                created.Errors.Select(e => e.Description).ToArray());

        // Self-registration always yields an end-user ("client").
        await _userManager.AddToRoleAsync(user, Roles.Client);

        return AccountResult.Success(await BuildAuthAsync(user));
    }

    public async Task<AccountResult> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
            return AccountResult.Fail(AccountErrorKind.Unauthorized, "Invalid email or password.");

        var check = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!check.Succeeded)
        {
            var message = check.IsLockedOut
                ? "Account is temporarily locked. Try again later."
                : "Invalid email or password.";
            return AccountResult.Fail(AccountErrorKind.Unauthorized, message);
        }

        return AccountResult.Success(await BuildAuthAsync(user));
    }

    public async Task<UserDto?> GetUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return null;

        return await BuildDtoAsync(user);
    }

    public async Task<UserDto?> UpdateProfileAsync(string userId, UpdateProfileRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return null;

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();

        await _userManager.UpdateAsync(user);

        return await BuildDtoAsync(user);
    }

    public async Task<UserDto?> AddAddressAsync(string userId, AddressRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return null;

        var existing = await _db.DeliveryAddresses.Where(a => a.UserId == user.Id).ToListAsync();

        var address = new DeliveryAddress
        {
            UserId = user.Id,
            Label = Clean(request.Label),
            AddressLine1 = Clean(request.AddressLine1),
            AddressLine2 = Clean(request.AddressLine2),
            City = Clean(request.City),
            State = Clean(request.State),
            Postcode = Clean(request.Postcode),
            DeliveryNotes = Clean(request.DeliveryNotes),
        };

        // The first address is always the default; otherwise honour the request.
        var makeDefault = request.IsDefault || existing.Count == 0;
        if (makeDefault)
        {
            foreach (var a in existing) a.IsDefault = false;
            address.IsDefault = true;
        }

        _db.DeliveryAddresses.Add(address);
        await _db.SaveChangesAsync();

        return await BuildDtoAsync(user);
    }

    public async Task<UserDto?> UpdateAddressAsync(string userId, int addressId, AddressRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return null;

        var addresses = await _db.DeliveryAddresses.Where(a => a.UserId == user.Id).ToListAsync();
        var address = addresses.FirstOrDefault(a => a.Id == addressId);
        if (address is null) return null;

        address.Label = Clean(request.Label);
        address.AddressLine1 = Clean(request.AddressLine1);
        address.AddressLine2 = Clean(request.AddressLine2);
        address.City = Clean(request.City);
        address.State = Clean(request.State);
        address.Postcode = Clean(request.Postcode);
        address.DeliveryNotes = Clean(request.DeliveryNotes);

        // Promoting to default clears the previous default. We never let the user
        // un-set the only/last default by editing — that's done via delete/set-default.
        if (request.IsDefault && !address.IsDefault)
        {
            foreach (var a in addresses) a.IsDefault = false;
            address.IsDefault = true;
        }

        await _db.SaveChangesAsync();

        return await BuildDtoAsync(user);
    }

    public async Task<UserDto?> DeleteAddressAsync(string userId, int addressId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return null;

        var addresses = await _db.DeliveryAddresses.Where(a => a.UserId == user.Id).ToListAsync();
        var address = addresses.FirstOrDefault(a => a.Id == addressId);
        if (address is null) return null;

        var wasDefault = address.IsDefault;
        _db.DeliveryAddresses.Remove(address);

        // If we removed the default, promote the most recently created remaining one.
        if (wasDefault)
        {
            var next = addresses
                .Where(a => a.Id != addressId)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefault();
            if (next is not null) next.IsDefault = true;
        }

        await _db.SaveChangesAsync();

        return await BuildDtoAsync(user);
    }

    public async Task<UserDto?> SetDefaultAddressAsync(string userId, int addressId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return null;

        var addresses = await _db.DeliveryAddresses.Where(a => a.UserId == user.Id).ToListAsync();
        if (addresses.All(a => a.Id != addressId)) return null;

        foreach (var a in addresses) a.IsDefault = a.Id == addressId;
        await _db.SaveChangesAsync();

        return await BuildDtoAsync(user);
    }

    public async Task<UserDto?> SetAvatarAsync(string userId, byte[] data, string contentType)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return null;

        // Upsert the 1:1 avatar row (bytes live in Postgres, not on disk).
        var avatar = await _db.UserAvatars.FirstOrDefaultAsync(a => a.UserId == user.Id);
        if (avatar is null)
        {
            avatar = new UserAvatar { UserId = user.Id };
            _db.UserAvatars.Add(avatar);
        }
        avatar.Data = data;
        avatar.ContentType = contentType;
        avatar.UpdatedAt = DateTimeOffset.UtcNow;

        // Retire any stale on-disk path so the DTO serves the new DB-backed image.
        user.AvatarUrl = null;

        await _db.SaveChangesAsync();
        await _userManager.UpdateAsync(user);

        return await BuildDtoAsync(user);
    }

    public async Task<(byte[] Data, string ContentType)?> GetAvatarAsync(int userId)
    {
        var avatar = await _db.UserAvatars
            .Where(a => a.UserId == userId)
            .Select(a => new { a.Data, a.ContentType })
            .FirstOrDefaultAsync();

        if (avatar is null || avatar.Data.Length == 0) return null;
        return (avatar.Data, string.IsNullOrEmpty(avatar.ContentType) ? "image/jpeg" : avatar.ContentType);
    }

    public async Task<AccountResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return AccountResult.Fail(AccountErrorKind.NotFound, "User not found.");

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (result.Succeeded)
            return AccountResult.Success();

        // A wrong current password surfaces as "PasswordMismatch".
        if (result.Errors.Any(e => e.Code == "PasswordMismatch"))
            return AccountResult.Fail(AccountErrorKind.Unauthorized, "Current password is incorrect.");

        return AccountResult.Fail(AccountErrorKind.Validation,
            result.Errors.Select(e => e.Description).ToArray());
    }

    // ---- Admin user management ----

    public async Task<PagedResult<AdminUserListItemDto>> ListUsersAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Case-insensitive contains over email / name. NormalizedEmail is already
            // upper-cased by Identity, so match the term against it for emails.
            var term = search.Trim();
            var upper = term.ToUpperInvariant();
            query = query.Where(u =>
                (u.NormalizedEmail != null && u.NormalizedEmail.Contains(upper)) ||
                u.FullName.ToUpper().Contains(upper));
        }

        var total = await query.CountAsync(ct);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = new List<AdminUserListItemDto>(users.Count);
        foreach (var u in users)
            items.Add(ToListItem(u, await _userManager.GetRolesAsync(u)));

        return new PagedResult<AdminUserListItemDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<AdminUserDetailDto?> GetUserDetailAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return new AdminUserDetailDto
        {
            User = await BuildDtoAsync(user, roles),
            IsAdmin = roles.Contains(Roles.Admin),
            IsDisabled = IsDisabled(user),
            CreatedAt = user.CreatedAt,
        };
    }

    public async Task<AdminActionResult> SetAdminRoleAsync(string userId, bool makeAdmin, string actingUserId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return AdminActionResult.Fail(AdminActionError.NotFound, "User not found.");

        var isAdmin = await _userManager.IsInRoleAsync(user, Roles.Admin);

        if (makeAdmin)
        {
            if (!isAdmin) await _userManager.AddToRoleAsync(user, Roles.Admin);
        }
        else
        {
            if (userId == actingUserId)
                return AdminActionResult.Fail(AdminActionError.SelfAction,
                    "You cannot remove your own admin role.");

            if (isAdmin)
            {
                var admins = await _userManager.GetUsersInRoleAsync(Roles.Admin);
                if (admins.Count <= 1)
                    return AdminActionResult.Fail(AdminActionError.LastAdmin,
                        "Cannot remove the last remaining admin.");

                await _userManager.RemoveFromRoleAsync(user, Roles.Admin);
            }
        }

        return AdminActionResult.Ok(ToListItem(user, await _userManager.GetRolesAsync(user)));
    }

    public async Task<AdminActionResult> SetUserDisabledAsync(string userId, bool disabled, string actingUserId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return AdminActionResult.Fail(AdminActionError.NotFound, "User not found.");

        if (disabled && userId == actingUserId)
            return AdminActionResult.Fail(AdminActionError.SelfAction,
                "You cannot disable your own account.");

        if (disabled)
        {
            // Lockout is only honoured at login if it's enabled for the user, so set both.
            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        }
        else
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
        }

        return AdminActionResult.Ok(ToListItem(user, await _userManager.GetRolesAsync(user)));
    }

    private static bool IsDisabled(ApplicationUser user) =>
        user.LockoutEnd is not null && user.LockoutEnd > DateTimeOffset.UtcNow;

    private static AdminUserListItemDto ToListItem(ApplicationUser user, IList<string> roles) => new()
    {
        Id = user.Id.ToString(),
        Email = user.Email ?? string.Empty,
        FullName = user.FullName,
        Roles = roles,
        IsAdmin = roles.Contains(Roles.Admin),
        IsDisabled = IsDisabled(user),
        CreatedAt = user.CreatedAt,
    };

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<AuthResponse> BuildAuthAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiresAt) = _tokenService.CreateToken(user, roles);

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = await BuildDtoAsync(user, roles),
        };
    }

    // Builds the UserDto including all delivery addresses and a convenience mirror
    // of the default address (so single-address clients keep working). Pass roles
    // when already loaded to save a query.
    private async Task<UserDto> BuildDtoAsync(ApplicationUser user, IList<string>? roles = null)
    {
        roles ??= await _userManager.GetRolesAsync(user);

        var addresses = await _db.DeliveryAddresses
            .Where(a => a.UserId == user.Id)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();

        // Prefer the DB-backed avatar (served from a streaming endpoint with a
        // cache-busting ?v token); fall back to any legacy on-disk path. We only
        // read the timestamp here — never the image bytes.
        var avatarUpdatedAt = await _db.UserAvatars
            .Where(a => a.UserId == user.Id)
            .Select(a => (DateTimeOffset?)a.UpdatedAt)
            .FirstOrDefaultAsync();

        var dto = new UserDto
        {
            Id = user.Id.ToString(),
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Roles = roles,
            Phone = user.PhoneNumber,
            AvatarUrl = avatarUpdatedAt is not null
                ? $"/api/auth/users/{user.Id}/avatar?v={avatarUpdatedAt.Value.Ticks}"
                : user.AvatarUrl,
            Addresses = addresses.Select(ToAddressDto).ToList(),
        };

        var def = addresses.FirstOrDefault(a => a.IsDefault) ?? addresses.FirstOrDefault();
        if (def is not null)
        {
            dto.DefaultAddressId = def.Id;
            dto.AddressLine1 = def.AddressLine1;
            dto.AddressLine2 = def.AddressLine2;
            dto.City = def.City;
            dto.State = def.State;
            dto.Postcode = def.Postcode;
            dto.DeliveryNotes = def.DeliveryNotes;
        }

        return dto;
    }

    private static DeliveryAddressDto ToAddressDto(DeliveryAddress a) => new()
    {
        Id = a.Id,
        Label = a.Label,
        AddressLine1 = a.AddressLine1,
        AddressLine2 = a.AddressLine2,
        City = a.City,
        State = a.State,
        Postcode = a.Postcode,
        DeliveryNotes = a.DeliveryNotes,
        IsDefault = a.IsDefault,
    };
}
