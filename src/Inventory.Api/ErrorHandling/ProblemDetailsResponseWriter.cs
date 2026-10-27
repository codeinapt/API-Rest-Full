using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.ErrorHandling;

public static class ProblemDetailsResponseWriter
{
    public const string ProblemJsonContentType = "application/problem+json";

    public static Task WriteAsync(HttpContext httpContext, ProblemDetails problemDetails, CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        return httpContext.Response.WriteAsJsonAsync(problemDetails, problemDetails.GetType(), options: null, ProblemJsonContentType, cancellationToken);
    }
}
