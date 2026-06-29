namespace Forkly.Api.Models;

// The two roles the landing page hands off as a UI hint (?role=client|admin).
// "client" is the default for self-registration; "admin" is seeded.
public static class Roles
{
    public const string Client = "client";
    public const string Admin = "admin";

    public static readonly string[] All = { Client, Admin };
}
