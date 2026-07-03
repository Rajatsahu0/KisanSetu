using Microsoft.AspNetCore.Authorization;

namespace KisanMitraAI.Core.Authorization;

/// <summary>
/// Authorization handler that validates role requirements.
/// </summary>
public class RoleRequirementHandler : AuthorizationHandler<RoleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoleRequirement requirement)
    {
        if (context.User == null)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        // Check if user has the required role
        if (context.User.IsInRole(requirement.Role))
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }

        return Task.CompletedTask;
    }
}
