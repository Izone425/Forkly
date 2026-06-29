using System.Security.Claims;
using Forkly.Api.Dtos;
using Forkly.Api.Grpc;
using Forkly.Api.Services;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace Forkly.Api.Services.Grpc;

// gRPC facade over IAccountService — the parallel of AuthController. Maps
// AccountResult to gRPC status codes via RpcException. The base class is
// generated from Protos/auth.proto by Grpc.Tools at build time.
public class AuthGrpcService : AuthService.AuthServiceBase
{
    private readonly IAccountService _accounts;

    public AuthGrpcService(IAccountService accounts)
    {
        _accounts = accounts;
    }

    public override async Task<AuthReply> Register(RegisterMsg request, ServerCallContext context)
    {
        var result = await _accounts.RegisterAsync(new RegisterRequest
        {
            FullName = request.FullName,
            Email = request.Email,
            Password = request.Password,
        });
        return ToReply(result);
    }

    public override async Task<AuthReply> Login(LoginMsg request, ServerCallContext context)
    {
        var result = await _accounts.LoginAsync(new LoginRequest
        {
            Email = request.Email,
            Password = request.Password,
        });
        return ToReply(result);
    }

    [Authorize]
    public override async Task<UserReply> GetCurrentUser(Empty request, ServerCallContext context)
    {
        var http = context.GetHttpContext();
        var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? http.User.FindFirstValue("sub");
        if (userId is null)
            throw new RpcException(new Status(StatusCode.Unauthenticated, "No user in token."));

        var user = await _accounts.GetUserAsync(userId);
        if (user is null)
            throw new RpcException(new Status(StatusCode.Unauthenticated, "User no longer exists."));

        return ToUserReply(user);
    }

    private static AuthReply ToReply(AccountResult result)
    {
        if (result.Succeeded)
        {
            var auth = result.Auth!;
            return new AuthReply
            {
                Token = auth.Token,
                ExpiresAt = auth.ExpiresAt.ToString("o"),
                User = ToUserReply(auth.User),
            };
        }

        var code = result.ErrorKind switch
        {
            AccountErrorKind.Conflict => StatusCode.AlreadyExists,
            AccountErrorKind.Unauthorized => StatusCode.Unauthenticated,
            AccountErrorKind.NotFound => StatusCode.NotFound,
            _ => StatusCode.InvalidArgument,
        };
        throw new RpcException(new Status(code, string.Join("; ", result.Errors)));
    }

    private static UserReply ToUserReply(UserDto user)
    {
        var reply = new UserReply
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
        };
        reply.Roles.AddRange(user.Roles);
        return reply;
    }
}
