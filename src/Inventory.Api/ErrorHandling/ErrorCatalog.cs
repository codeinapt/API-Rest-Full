using System.Collections.Frozen;
using Inventory.Application.Common.Messages;
using Inventory.Domain.Common;

namespace Inventory.Api.ErrorHandling;

public static class ErrorCatalog
{
    public static IReadOnlyDictionary<ErrorCode, ErrorText> Texts { get; } = new Dictionary<ErrorCode, ErrorText>
    {
        [ErrorCode.ValidationError] = new(ErrorMessages.ValidationErrorTitle, ErrorMessages.ValidationError),
        [ErrorCode.Unauthorized] = new(ErrorMessages.UnauthorizedTitle, ErrorMessages.Unauthorized),
        [ErrorCode.Forbidden] = new(ErrorMessages.AccessDeniedTitle, ErrorMessages.Forbidden),
        [ErrorCode.AreaAccessDenied] = new(ErrorMessages.AccessDeniedTitle, ErrorMessages.AreaAccessDenied),
        [ErrorCode.AreaNotFound] = new(ErrorMessages.NotFoundTitle, ErrorMessages.AreaNotFound),
        [ErrorCode.CategoryNotFound] = new(ErrorMessages.NotFoundTitle, ErrorMessages.CategoryNotFound),
        [ErrorCode.ProductNotFound] = new(ErrorMessages.NotFoundTitle, ErrorMessages.ProductNotFound),
        [ErrorCode.AreaCodeAlreadyExists] = new(ErrorMessages.ConflictTitle, ErrorMessages.AreaCodeAlreadyExists),
        [ErrorCode.CategoryNameAlreadyExists] = new(ErrorMessages.ConflictTitle, ErrorMessages.CategoryNameAlreadyExists),
        [ErrorCode.SkuAlreadyExists] = new(ErrorMessages.ConflictTitle, ErrorMessages.SkuAlreadyExists),
        [ErrorCode.AreaHasActiveCategories] = new(ErrorMessages.ConflictTitle, ErrorMessages.AreaHasActiveCategories),
        [ErrorCode.CategoryHasActiveProducts] = new(ErrorMessages.ConflictTitle, ErrorMessages.CategoryHasActiveProducts),
        [ErrorCode.AreaInactive] = new(ErrorMessages.ConflictTitle, ErrorMessages.AreaInactive),
        [ErrorCode.CategoryInactive] = new(ErrorMessages.ConflictTitle, ErrorMessages.CategoryInactive),
        [ErrorCode.ProductInactive] = new(ErrorMessages.ConflictTitle, ErrorMessages.ProductInactive),
        [ErrorCode.ProductAlreadyActive] = new(ErrorMessages.ConflictTitle, ErrorMessages.ProductAlreadyActive),
        [ErrorCode.ProductHasStock] = new(ErrorMessages.ConflictTitle, ErrorMessages.ProductHasStock),
        [ErrorCode.InsufficientStock] = new(ErrorMessages.InventoryConflictTitle, ErrorMessages.InsufficientStock),
        [ErrorCode.ReferencedAreaInvalid] = new(ErrorMessages.InvalidReferenceTitle, ErrorMessages.ReferencedAreaInvalid),
        [ErrorCode.ReferencedCategoryInvalid] = new(ErrorMessages.InvalidReferenceTitle, ErrorMessages.ReferencedCategoryInvalid),
        [ErrorCode.RelatedMovementInvalid] = new(ErrorMessages.InvalidReferenceTitle, ErrorMessages.RelatedMovementInvalid),
        [ErrorCode.InternalError] = new(ErrorMessages.InternalErrorTitle, ErrorMessages.InternalError),
    }.ToFrozenDictionary();
}
