using Microsoft.AspNetCore.Authorization;

namespace KisanMitraAI.Core.Authorization;

/// <summary>
/// Authorization attribute that requires the user to be authenticated as an administrator.
/// Ensures the user has the "Admin" role for administrative operations.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class RequiresAdminAttribute : AuthorizeAttribute
{
    public RequiresAdminAttribute()
    {
        Policy = "RequiresAdmin";
    }
}
