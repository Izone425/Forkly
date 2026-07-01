using System.Security.Claims;
using Forkly.Api.Dtos;
using Forkly.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forkly.Api.Controllers;

// REST facade over IAccountService. Thin: it only maps AccountResult to HTTP
// status codes. The gRPC service (Services/Grpc/AuthGrpcService) is the parallel
// facade over the same logic.
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAccountService _accounts;
    private readonly IWebHostEnvironment _env;

    public AuthController(IAccountService accounts, IWebHostEnvironment env)
    {
        _accounts = accounts;
        _env = env;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _accounts.RegisterAsync(request);
        return ToResponse(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _accounts.LoginAsync(request);
        return ToResponse(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();

        var user = await _accounts.GetUserAsync(userId);
        return user is null ? Unauthorized() : Ok(user);
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();

        var user = await _accounts.UpdateProfileAsync(userId, request);
        return user is null ? Unauthorized() : Ok(user);
    }

    // ---- Delivery addresses ---- (all return the full updated UserDto)

    [Authorize]
    [HttpPost("me/addresses")]
    public async Task<IActionResult> AddAddress(AddressRequest request)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();

        var user = await _accounts.AddAddressAsync(userId, request);
        return user is null ? Unauthorized() : Ok(user);
    }

    [Authorize]
    [HttpPut("me/addresses/{id:int}")]
    public async Task<IActionResult> UpdateAddress(int id, AddressRequest request)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();

        var user = await _accounts.UpdateAddressAsync(userId, id, request);
        return user is null ? NotFound() : Ok(user);
    }

    [Authorize]
    [HttpDelete("me/addresses/{id:int}")]
    public async Task<IActionResult> DeleteAddress(int id)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();

        var user = await _accounts.DeleteAddressAsync(userId, id);
        return user is null ? NotFound() : Ok(user);
    }

    [Authorize]
    [HttpPut("me/addresses/{id:int}/default")]
    public async Task<IActionResult> SetDefaultAddress(int id)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();

        var user = await _accounts.SetDefaultAddressAsync(userId, id);
        return user is null ? NotFound() : Ok(user);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await _accounts.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
        return result.Succeeded ? NoContent() : ToResponse(result);
    }

    [Authorize]
    [HttpPost("me/avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();

        if (file is null || file.Length == 0)
            return BadRequest(new { errors = new[] { "No file uploaded." } });
        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new { errors = new[] { "Image must be 10 MB or smaller." } });

        var allowed = new Dictionary<string, string>
        {
            ["image/png"] = ".png",
            ["image/jpeg"] = ".jpg",
            ["image/webp"] = ".webp",
        };
        if (!allowed.ContainsKey(file.ContentType))
            return BadRequest(new { errors = new[] { "Only PNG, JPEG or WebP images are allowed." } });

        // Store the bytes in Postgres (bytea) rather than on disk.
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);

        var user = await _accounts.SetAvatarAsync(userId, ms.ToArray(), file.ContentType);
        return user is null ? Unauthorized() : Ok(user);
    }

    // Anonymous so <img> tags (which can't send an Authorization header) can load
    // it. The cache-busting ?v token on the URL keeps updated avatars fresh.
    [AllowAnonymous]
    [HttpGet("users/{id:int}/avatar")]
    public async Task<IActionResult> GetAvatar(int id)
    {
        var img = await _accounts.GetAvatarAsync(id);
        return img is null ? NotFound() : File(img.Value.Data, img.Value.ContentType);
    }

    private string? CurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

    private IActionResult ToResponse(AccountResult result)
    {
        if (result.Succeeded)
            return Ok(result.Auth);

        var problem = new { errors = result.Errors };
        return result.ErrorKind switch
        {
            AccountErrorKind.Conflict => Conflict(problem),
            AccountErrorKind.Unauthorized => Unauthorized(problem),
            AccountErrorKind.NotFound => NotFound(problem),
            _ => BadRequest(problem),
        };
    }
}
