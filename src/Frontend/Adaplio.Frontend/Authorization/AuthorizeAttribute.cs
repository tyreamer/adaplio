using Microsoft.AspNetCore.Components;

namespace Adaplio.Frontend.Authorization;

/// <summary>
/// Attribute to specify authorization requirements for components and pages
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class AuthorizeAttribute : Attribute
{
    public string? Roles { get; set; }
    public string? Policy { get; set; }
    public bool RequireAuthentication { get; set; } = true;
}

/// <summary>
/// Shorthand attribute for client-only access
/// </summary>
public class ClientOnlyAttribute : AuthorizeAttribute
{
    public ClientOnlyAttribute()
    {
        Roles = "client";
    }
}

/// <summary>
/// Shorthand attribute for trainer-only access
/// </summary>
public class TrainerOnlyAttribute : AuthorizeAttribute
{
    public TrainerOnlyAttribute()
    {
        Roles = "trainer";
    }
}