using Microsoft.AspNetCore.Authorization;

namespace KisanMitraAI.Core.Authorization;

/// <summary>
/// Authorization requirement that checks for a specific role.
/// </summary>
public class RoleRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the required role name.
    /// </summary>
    public string Role { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleRequirement"/> class.
    /// </summary>
    /// <param name="role">The required role name.</param>
    public RoleRequirement(string role)
    {
        Role = role ?? throw new ArgumentNullException(nameof(role));
    }
}
