using Microsoft.AspNetCore.Authorization;

namespace KisanMitraAI.Core.Authorization;

/// <summary>
/// Authorization attribute that requires the user to be authenticated as a farmer.
/// Ensures the user has the "Farmer" role and can only access their own data.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class RequiresFarmerAttribute : AuthorizeAttribute
{
    public RequiresFarmerAttribute()
    {
        Policy = "RequiresFarmer";
    }
}
