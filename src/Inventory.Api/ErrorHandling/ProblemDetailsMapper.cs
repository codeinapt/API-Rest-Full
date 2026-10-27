using System.Globalization;
using System.Text.RegularExpressions;
using FluentValidation;
using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Messages;
using Inventory.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Inventory.Api.ErrorHandling;

public static partial class ProblemDetailsMapper
{
    private const string TypeBaseUri = "https://httpstatuses.io/";
    private const string CodeExtensionName = "code";

    public static ProblemDetails Map(Exception exception)
    {
        return exception switch
        {
            ValidationException validationException => MapValidation(validationException),
            NotFoundException notFoundException => MapCoded(notFoundException, StatusCodes.Status404NotFound),
            ForbiddenException forbiddenException => MapCoded(forbiddenException, StatusCodes.Status403Forbidden),
            InvalidReferenceException invalidReferenceException => MapCoded(invalidReferenceException, StatusCodes.Status422UnprocessableEntity),
            DomainException domainException => MapCoded(domainException, StatusCodes.Status409Conflict),
            _ => Describe(new ProblemDetails(), StatusCodes.Status500InternalServerError, ErrorCode.InternalError),
        };
    }

    public static ProblemDetails MapUnauthorized()
    {
        return Describe(new ProblemDetails(), StatusCodes.Status401Unauthorized, ErrorCode.Unauthorized);
    }

    public static ProblemDetails MapForbidden()
    {
        return Describe(new ProblemDetails(), StatusCodes.Status403Forbidden, ErrorCode.Forbidden);
    }

    public static ValidationProblemDetails MapInvalidModelState(ModelStateDictionary modelState)
    {
        var errors = modelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .ToDictionary(entry => entry.Key, _ => new[] { ErrorMessages.FieldValueInvalid });

        return Describe(new ValidationProblemDetails(errors), StatusCodes.Status400BadRequest, ErrorCode.ValidationError);
    }

    private static ValidationProblemDetails MapValidation(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).ToArray());

        return Describe(new ValidationProblemDetails(errors), StatusCodes.Status400BadRequest, ErrorCode.ValidationError);
    }

    private static ProblemDetails MapCoded(InventoryException exception, int statusCode)
    {
        var problemDetails = Describe(new ProblemDetails(), statusCode, exception.Code);
        problemDetails.Detail = string.Format(CultureInfo.InvariantCulture, problemDetails.Detail!, exception.MessageArguments.ToArray());

        return problemDetails;
    }

    private static TProblemDetails Describe<TProblemDetails>(TProblemDetails problemDetails, int statusCode, ErrorCode code)
        where TProblemDetails : ProblemDetails
    {
        var errorText = ErrorCatalog.Texts[code];
        problemDetails.Type = TypeBaseUri + statusCode.ToString(CultureInfo.InvariantCulture);
        problemDetails.Status = statusCode;
        problemDetails.Title = errorText.Title;
        problemDetails.Detail = errorText.DetailTemplate;
        problemDetails.Extensions[CodeExtensionName] = ToWireCode(code);

        return problemDetails;
    }

    private static string ToWireCode(ErrorCode code)
    {
        return WordBoundary().Replace(code.ToString(), "_").ToUpperInvariant();
    }

    [GeneratedRegex("(?<=[a-z])(?=[A-Z])")]
    private static partial Regex WordBoundary();
}
