using System.Globalization;
using FluentValidation;
using FluentValidation.Results;
using Inventory.Api.ErrorHandling;
using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Messages;
using Inventory.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Inventory.Api.Tests.ErrorHandling;

public sealed class ProblemDetailsMapperTests
{
    public static readonly TheoryData<InventoryException, int, string, string, string> CodedExceptions = new()
    {
        { new ForbiddenException(ErrorCode.AreaAccessDenied, "FOOD"), 403, "AREA_ACCESS_DENIED", ErrorMessages.AccessDeniedTitle, ErrorMessages.AreaAccessDenied },
        { new NotFoundException(ErrorCode.AreaNotFound, 7), 404, "AREA_NOT_FOUND", ErrorMessages.NotFoundTitle, ErrorMessages.AreaNotFound },
        { new NotFoundException(ErrorCode.CategoryNotFound, 8), 404, "CATEGORY_NOT_FOUND", ErrorMessages.NotFoundTitle, ErrorMessages.CategoryNotFound },
        { new NotFoundException(ErrorCode.ProductNotFound, 9), 404, "PRODUCT_NOT_FOUND", ErrorMessages.NotFoundTitle, ErrorMessages.ProductNotFound },
        { new DomainException(ErrorCode.AreaCodeAlreadyExists, "FOOD"), 409, "AREA_CODE_ALREADY_EXISTS", ErrorMessages.ConflictTitle, ErrorMessages.AreaCodeAlreadyExists },
        { new DomainException(ErrorCode.CategoryNameAlreadyExists, "Dairy"), 409, "CATEGORY_NAME_ALREADY_EXISTS", ErrorMessages.ConflictTitle, ErrorMessages.CategoryNameAlreadyExists },
        { new DomainException(ErrorCode.SkuAlreadyExists, "FOOD-001"), 409, "SKU_ALREADY_EXISTS", ErrorMessages.ConflictTitle, ErrorMessages.SkuAlreadyExists },
        { new DomainException(ErrorCode.AreaHasActiveCategories), 409, "AREA_HAS_ACTIVE_CATEGORIES", ErrorMessages.ConflictTitle, ErrorMessages.AreaHasActiveCategories },
        { new DomainException(ErrorCode.CategoryHasActiveProducts), 409, "CATEGORY_HAS_ACTIVE_PRODUCTS", ErrorMessages.ConflictTitle, ErrorMessages.CategoryHasActiveProducts },
        { new DomainException(ErrorCode.AreaInactive, 3), 409, "AREA_INACTIVE", ErrorMessages.ConflictTitle, ErrorMessages.AreaInactive },
        { new DomainException(ErrorCode.CategoryInactive, 4), 409, "CATEGORY_INACTIVE", ErrorMessages.ConflictTitle, ErrorMessages.CategoryInactive },
        { new DomainException(ErrorCode.ProductInactive, 5), 409, "PRODUCT_INACTIVE", ErrorMessages.ConflictTitle, ErrorMessages.ProductInactive },
        { new DomainException(ErrorCode.ProductAlreadyActive, 6), 409, "PRODUCT_ALREADY_ACTIVE", ErrorMessages.ConflictTitle, ErrorMessages.ProductAlreadyActive },
        { new DomainException(ErrorCode.ProductHasStock, 12), 409, "PRODUCT_HAS_STOCK", ErrorMessages.ConflictTitle, ErrorMessages.ProductHasStock },
        { new DomainException(ErrorCode.InsufficientStock, 3, 5), 409, "INSUFFICIENT_STOCK", ErrorMessages.InventoryConflictTitle, ErrorMessages.InsufficientStock },
        { new InvalidReferenceException(ErrorCode.ReferencedAreaInvalid, 2), 422, "REFERENCED_AREA_INVALID", ErrorMessages.InvalidReferenceTitle, ErrorMessages.ReferencedAreaInvalid },
        { new InvalidReferenceException(ErrorCode.ReferencedCategoryInvalid, 4), 422, "REFERENCED_CATEGORY_INVALID", ErrorMessages.InvalidReferenceTitle, ErrorMessages.ReferencedCategoryInvalid },
        { new InvalidReferenceException(ErrorCode.RelatedMovementInvalid, 99), 422, "RELATED_MOVEMENT_INVALID", ErrorMessages.InvalidReferenceTitle, ErrorMessages.RelatedMovementInvalid },
    };

    [Theory]
    [MemberData(nameof(CodedExceptions))]
    public void Map_WhenExceptionCarriesAnErrorCode_ReturnsItsStatusCodeAndTexts(InventoryException exception, int expectedStatus, string expectedCode, string expectedTitle, string detailTemplate)
    {
        var problemDetails = ProblemDetailsMapper.Map(exception);
        problemDetails.Status.ShouldBe(expectedStatus);
        problemDetails.Type.ShouldBe($"https://httpstatuses.io/{expectedStatus}");
        problemDetails.Extensions["code"].ShouldBe(expectedCode);
        problemDetails.Title.ShouldBe(expectedTitle);
        problemDetails.Detail.ShouldBe(FormatDetail(detailTemplate, exception));
    }

    [Fact]
    public void Map_WhenExceptionIsValidationException_Returns400WithValidationCodeAndTexts()
    {
        var problemDetails = ProblemDetailsMapper.Map(CreateValidationException());
        problemDetails.Status.ShouldBe(400);
        problemDetails.Type.ShouldBe("https://httpstatuses.io/400");
        problemDetails.Extensions["code"].ShouldBe("VALIDATION_ERROR");
        problemDetails.Title.ShouldBe(ErrorMessages.ValidationErrorTitle);
        problemDetails.Detail.ShouldBe(ErrorMessages.ValidationError);
    }

    [Fact]
    public void Map_WhenExceptionIsValidationException_GroupsErrorMessagesByProperty()
    {
        var problemDetails = ProblemDetailsMapper.Map(CreateValidationException());
        var errors = problemDetails.ShouldBeOfType<ValidationProblemDetails>().Errors;
        errors.Keys.ShouldBe(["Name", "Sku"], ignoreOrder: true);
        errors["Name"].ShouldBe(["Name is required.", "Name is too long."]);
        errors["Sku"].ShouldBe(["Sku is required."]);
    }

    [Fact]
    public void MapInvalidModelState_Returns400WithValidationCodeAndTexts()
    {
        var problemDetails = ProblemDetailsMapper.MapInvalidModelState(CreateInvalidModelState());
        problemDetails.Status.ShouldBe(400);
        problemDetails.Type.ShouldBe("https://httpstatuses.io/400");
        problemDetails.Extensions["code"].ShouldBe("VALIDATION_ERROR");
        problemDetails.Title.ShouldBe(ErrorMessages.ValidationErrorTitle);
        problemDetails.Detail.ShouldBe(ErrorMessages.ValidationError);
    }

    [Fact]
    public void MapInvalidModelState_ReplacesFrameworkMessagesWithOneSpanishMessagePerInvalidField()
    {
        var errors = ProblemDetailsMapper.MapInvalidModelState(CreateInvalidModelState()).Errors;
        errors.Keys.ShouldBe(["Page", "Status"], ignoreOrder: true);
        errors["Page"].ShouldBe([ErrorMessages.FieldValueInvalid]);
        errors["Status"].ShouldBe([ErrorMessages.FieldValueInvalid]);
    }

    [Fact]
    public void Map_WhenExceptionIsUnexpected_Returns500WithInternalErrorCodeAndTexts()
    {
        var problemDetails = ProblemDetailsMapper.Map(new InvalidOperationException("Connection string leaked"));
        problemDetails.Status.ShouldBe(500);
        problemDetails.Type.ShouldBe("https://httpstatuses.io/500");
        problemDetails.Extensions["code"].ShouldBe("INTERNAL_ERROR");
        problemDetails.Title.ShouldBe(ErrorMessages.InternalErrorTitle);
        problemDetails.Detail.ShouldBe(ErrorMessages.InternalError);
    }

    [Fact]
    public void MapUnauthorized_Returns401WithUnauthorizedCodeAndTexts()
    {
        var problemDetails = ProblemDetailsMapper.MapUnauthorized();
        problemDetails.Status.ShouldBe(401);
        problemDetails.Type.ShouldBe("https://httpstatuses.io/401");
        problemDetails.Extensions["code"].ShouldBe("UNAUTHORIZED");
        problemDetails.Title.ShouldBe(ErrorMessages.UnauthorizedTitle);
        problemDetails.Detail.ShouldBe(ErrorMessages.Unauthorized);
    }

    [Fact]
    public void MapForbidden_Returns403WithForbiddenCodeAndTexts()
    {
        var problemDetails = ProblemDetailsMapper.MapForbidden();
        problemDetails.Status.ShouldBe(403);
        problemDetails.Type.ShouldBe("https://httpstatuses.io/403");
        problemDetails.Extensions["code"].ShouldBe("FORBIDDEN");
        problemDetails.Title.ShouldBe(ErrorMessages.AccessDeniedTitle);
        problemDetails.Detail.ShouldBe(ErrorMessages.Forbidden);
    }

    private static string FormatDetail(string detailTemplate, InventoryException exception)
    {
        return string.Format(CultureInfo.InvariantCulture, detailTemplate, exception.MessageArguments.ToArray());
    }

    private static ModelStateDictionary CreateInvalidModelState()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Page", "The value 'abc' is not valid for Page.");
        modelState.AddModelError("Status", "The value 'foo' is not valid.");
        modelState.AddModelError("Status", "The Status field is invalid.");
        modelState.SetModelValue("PageSize", "20", "20");

        return modelState;
    }

    private static ValidationException CreateValidationException()
    {
        return new ValidationException(
        [
            new ValidationFailure("Name", "Name is required."),
            new ValidationFailure("Name", "Name is too long."),
            new ValidationFailure("Sku", "Sku is required."),
        ]);
    }
}
