using System.Text;
using Forkly.MenuService.Data;
using Forkly.MenuService.Repositories;
using Forkly.MenuService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// --- Database: the shared SIT "foodorder" DB, but our own isolated "menu" schema. ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection is not configured (see appsettings.Development.json).");

builder.Services.AddDbContext<MenuDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
        npgsql.MigrationsHistoryTable("__EFMigrationsHistory", MenuDbContext.Schema)));

// --- Auth: accept JWTs issued by the User service (Forkly-Api). Same key/issuer/audience
//     so a token from the existing login works here unchanged. The Menu Service issues
//     no tokens of its own — it only validates and authorises admin write endpoints. ---
var jwt = builder.Configuration.GetSection("Jwt");
var jwtKey = jwt["Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Jwt:Key is not configured (must match the User service).");

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
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
    });
builder.Services.AddAuthorization();

// --- Clean architecture wiring: Controller -> Service -> Repository -> DbContext. ---
builder.Services.AddScoped<IMenuRepository, MenuRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Allow the Vue frontend (any localhost dev port) to call this service.
const string CorsPolicy = "frontend";
builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
    policy.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

// Apply migrations and seed the sample menu on startup so a fresh clone has data.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MenuDbContext>();
    await db.Database.MigrateAsync();
    await MenuSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
