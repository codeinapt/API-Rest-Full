namespace Inventory.Domain.Common;

public enum ErrorCode
{
    ValidationError,
    Unauthorized,
    Forbidden,
    AreaAccessDenied,
    AreaNotFound,
    CategoryNotFound,
    ProductNotFound,
    AreaCodeAlreadyExists,
    CategoryNameAlreadyExists,
    SkuAlreadyExists,
    AreaHasActiveCategories,
    CategoryHasActiveProducts,
    AreaInactive,
    CategoryInactive,
    ProductInactive,
    ProductAlreadyActive,
    ProductHasStock,
    InsufficientStock,
    ReferencedAreaInvalid,
    ReferencedCategoryInvalid,
    RelatedMovementInvalid,
    InternalError,
}
