using System.Text;
using Forkly.Api.Data;
using Forkly.Api.Models;
using Forkly.Api.Services;
using Forkly.Api.Services.Grpc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Two deterministic dev endpoints:
//   5080 — HTTP/1.1: REST + gRPC-web (what the browser/auth app uses).
//   5081 — HTTP/2 cleartext (h2c): native gRPC for service-to-service clients
//          and grpcurl. (h2c needs an HTTP/2-only endpoint; a mixed cleartext
//          endpoint can't negotiate HTTP/2 without TLS, so it falls back to 1.1.)
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5080, listen => listen.Protocols = HttpProtocols.Http1AndHttp2);
    options.ListenLocalhost(5081, listen => listen.Protocols = HttpProtocols.Http2);
});

var corsPolicy = "forkly-frontends";
var allowedOrigins = new[] { "http://localhost:5174", "http://localhost:5173" };

// ---- Persistence ----
// PostgreSQL is the sole provider — connection string lives in appsettings.json
// (overridden per-environment, e.g. the SIT DB in appsettings.Development.json).
// See DATABASE.md.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---- Identity ----
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
    {
        // Relaxed policy for a training project — tighten for production.
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireDigit = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// ---- JWT auth ----
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
        };
    });

builder.Services.AddAuthorization();

// ---- App services ----
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IAccountService, AccountService>();

// ---- Transports: REST + gRPC ----
builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicy, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        // Needed so gRPC-web clients can read the trailing status.
        .WithExposedHeaders("grpc-status", "grpc-message", "grpc-status-details-bin"));
});

// Ensure wwwroot/uploads exists BEFORE the host is built, so the static-file
// provider binds to it (a missing wwwroot at build time disables UseStaticFiles).
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "uploads"));

var app = builder.Build();

// ---- Migrate + seed on startup ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
}

// Serve uploaded avatars (and any other wwwroot content).
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(corsPolicy);
app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireCors(corsPolicy);
app.MapGrpcService<AuthGrpcService>().EnableGrpcWeb().RequireCors(corsPolicy);

app.Run();
