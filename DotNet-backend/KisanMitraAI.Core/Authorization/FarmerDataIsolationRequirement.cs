using Microsoft.AspNetCore.Authorization;

namespace KisanMitraAI.Core.Authorization;

/// <summary>
/// Authorization requirement that ensures farmers can only access their own data.
/// This requirement is used in conjunction with FarmerDataIsolationHandler.
/// </summary>
public class FarmerDataIsolationRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets a value indicating whether to allow admin users to bypass data isolation.
    /// </summary>
    public bool AllowAdminBypass { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FarmerDataIsolationRequirement"/> class.
    /// </summary>
    /// <param name="allowAdminBypass">Whether to allow admin users to bypass data isolation checks.</param>
    public FarmerDataIsolationRequirement(bool allowAdminBypass = true)
    {
        AllowAdminBypass = allowAdminBypass;
    }
}
