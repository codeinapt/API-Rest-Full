using Inventory.Api.ErrorHandling;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Inventory.Api.Tests.ErrorHandling;

public sealed class ProblemDetailsAuthorizationResultHandlerTests
{
    private readonly AuthorizationPolicy policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

    [Fact]
    public async Task HandleAsync_WhenRequestIsChallenged_WritesUnauthorizedProblemDetails()
    {
        var httpContext = CreateContextWithAuthentication();
        await new ProblemDetailsAuthorizationResultHandler().HandleAsync(_ => Task.CompletedTask, httpContext, policy, PolicyAuthorizationResult.Challenge());
        var body = await HttpResponseReader.ReadJsonBodyAsync(httpContext.Response);
        httpContext.Response.StatusCode.ShouldBe(401);
        httpContext.Response.ContentType.ShouldBe("application/problem+json");
        body.GetProperty("code").GetString().ShouldBe("UNAUTHORIZED");
    }

    [Fact]
    public async Task HandleAsync_WhenRequestIsForbidden_WritesForbiddenProblemDetails()
    {
        var httpContext = CreateContextWithAuthentication();
        await new ProblemDetailsAuthorizationResultHandler().HandleAsync(_ => Task.CompletedTask, httpContext, policy, PolicyAuthorizationResult.Forbid());
        var body = await HttpResponseReader.ReadJsonBodyAsync(httpContext.Response);
        httpContext.Response.StatusCode.ShouldBe(403);
        httpContext.Response.ContentType.ShouldBe("application/problem+json");
        body.GetProperty("code").GetString().ShouldBe("FORBIDDEN");
    }

    [Fact]
    public async Task HandleAsync_WhenAuthorizationSucceeds_CallsNextWithoutWritingProblemDetails()
    {
        var httpContext = CreateContextWithAuthentication();
        var next = Substitute.For<RequestDelegate>();
        await new ProblemDetailsAuthorizationResultHandler().HandleAsync(next, httpContext, policy, PolicyAuthorizationResult.Success());
        await next.Received(1).Invoke(httpContext);
        httpContext.Response.Body.Length.ShouldBe(0);
    }

    private static HttpContext CreateContextWithAuthentication()
    {
        var httpContext = HttpResponseReader.CreateContextWithReadableBody();
        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton(Substitute.For<IAuthenticationService>())
            .BuildServiceProvider();

        return httpContext;
    }
}
