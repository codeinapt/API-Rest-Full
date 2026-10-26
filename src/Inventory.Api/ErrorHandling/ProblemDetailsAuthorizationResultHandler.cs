using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Inventory.Api.ErrorHandling;

public sealed class ProblemDetailsAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler defaultHandler = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        await defaultHandler.HandleAsync(next, context, policy, authorizeResult);
        if (authorizeResult.Challenged)
        {
            await ProblemDetailsResponseWriter.WriteAsync(context, ProblemDetailsMapper.MapUnauthorized(), context.RequestAborted);
        }
        if (authorizeResult.Forbidden)
        {
            await ProblemDetailsResponseWriter.WriteAsync(context, ProblemDetailsMapper.MapForbidden(), context.RequestAborted);
        }
    }
}
