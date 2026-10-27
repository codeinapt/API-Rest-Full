using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.ErrorHandling;

public static class InvalidModelStateResponseFactory
{
    public static IActionResult Create(ActionContext actionContext)
    {
        var problemDetails = ProblemDetailsMapper.MapInvalidModelState(actionContext.ModelState);

        return new ObjectResult(problemDetails)
        {
            StatusCode = problemDetails.Status,
            ContentTypes = { ProblemDetailsResponseWriter.ProblemJsonContentType },
        };
    }
}
