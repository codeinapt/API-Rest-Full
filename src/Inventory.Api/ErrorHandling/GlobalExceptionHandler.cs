using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.ErrorHandling;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = ProblemDetailsMapper.Map(exception);
        LogException(exception, problemDetails);
        await ProblemDetailsResponseWriter.WriteAsync(httpContext, problemDetails, cancellationToken);

        return true;
    }

    private void LogException(Exception exception, ProblemDetails problemDetails)
    {
        if (problemDetails.Status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception while processing the request.");
        }
        else
        {
            logger.LogInformation("Request rejected with {StatusCode} by {ExceptionType}.", problemDetails.Status, exception.GetType().Name);
        }
    }
}
