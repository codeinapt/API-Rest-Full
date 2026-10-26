using Microsoft.AspNetCore.Authorization;

namespace Inventory.Api.Authorization;

public static class InventoryPolicies
{
    public static AuthorizationBuilder AddInventoryPolicies(this AuthorizationBuilder builder)
    {
        return builder
            .AddPolicy(PolicyNames.CanRead, policy => RequireScope(policy, ScopeNames.Read))
            .AddPolicy(PolicyNames.CanOperate, policy => RequireScope(policy, ScopeNames.Write).RequireRole(RoleNames.Operator, RoleNames.Supervisor))
            .AddPolicy(PolicyNames.CanSupervise, policy => RequireScope(policy, ScopeNames.Write).RequireRole(RoleNames.Supervisor))
            .AddPolicy(PolicyNames.CanAdminister, policy => RequireScope(policy, ScopeNames.Write).RequireRole(RoleNames.Admin));
    }

    private static AuthorizationPolicyBuilder RequireScope(AuthorizationPolicyBuilder policy, string scope)
    {
        return policy
            .RequireAuthenticatedUser()
            .RequireAssertion(context => context.User.HasScope(scope));
    }
}
