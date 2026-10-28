using Inventory.Api.ErrorHandling;
using Inventory.Application.Common.Messages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;

namespace Inventory.Api.Tests.ErrorHandling;

public sealed class InvalidModelStateResponseFactoryTests
{
    [Fact]
    public void Create_ReturnsBadRequestValidationProblemAsProblemJson()
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        actionContext.ModelState.AddModelError("Status", "The value 'foo' is not valid.");
        var result = InvalidModelStateResponseFactory.Create(actionContext).ShouldBeOfType<ObjectResult>();
        result.ShouldSatisfyAllConditions(
            () => result.StatusCode.ShouldBe(StatusCodes.Status400BadRequest),
            () => result.ContentTypes.ShouldBe(["application/problem+json"]),
            () => result.Value.ShouldBeOfType<ValidationProblemDetails>().Errors["Status"].ShouldBe([ErrorMessages.FieldValueInvalid]));
    }
}
