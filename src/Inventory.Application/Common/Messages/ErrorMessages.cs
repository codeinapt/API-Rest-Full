namespace Inventory.Application.Common.Messages;

public static class ErrorMessages
{
    public const string ValidationErrorTitle = "Datos no válidos";
    public const string UnauthorizedTitle = "No autenticado";
    public const string AccessDeniedTitle = "Acceso denegado";
    public const string NotFoundTitle = "Recurso no encontrado";
    public const string ConflictTitle = "Conflicto";
    public const string InventoryConflictTitle = "Conflicto de inventario";
    public const string InvalidReferenceTitle = "Referencia no válida";
    public const string InternalErrorTitle = "Error interno";
    public const string ValidationError = "Uno o más campos no son válidos.";
    public const string Unauthorized = "Debe iniciar sesión para acceder a este recurso.";
    public const string Forbidden = "No tiene permisos para realizar esta operación.";
    public const string AreaAccessDenied = "No es supervisor del área {0}.";
    public const string AreaNotFound = "No existe el área {0}.";
    public const string CategoryNotFound = "No existe la categoría {0}.";
    public const string ProductNotFound = "No existe el producto {0}.";
    public const string AreaCodeAlreadyExists = "Ya existe un área activa con el código {0}.";
    public const string CategoryNameAlreadyExists = "Ya existe una categoría activa con el nombre {0}.";
    public const string SkuAlreadyExists = "Ya existe un producto activo con el SKU {0}.";
    public const string AreaHasActiveCategories = "El área tiene categorías activas.";
    public const string CategoryHasActiveProducts = "La categoría tiene productos activos.";
    public const string AreaInactive = "El área {0} está inactiva.";
    public const string CategoryInactive = "La categoría {0} está inactiva.";
    public const string ProductInactive = "El producto {0} está inactivo.";
    public const string ProductAlreadyActive = "El producto {0} ya está activo.";
    public const string ProductHasStock = "El producto tiene {0} unidades. Use la desactivación con motivo.";
    public const string InsufficientStock = "Stock insuficiente: disponible {0}, solicitado {1}.";
    public const string ReferencedAreaInvalid = "El área {0} no existe o está inactiva.";
    public const string ReferencedCategoryInvalid = "La categoría {0} no existe o está inactiva.";
    public const string RelatedMovementInvalid = "El movimiento {0} no existe o no pertenece al producto.";
    public const string InternalError = "Ocurrió un error inesperado.";
    public const string AreaCodeInvalid = "El código debe tener entre 2 y 10 letras mayúsculas (A-Z).";
    public const string AreaNameInvalid = "El nombre debe tener entre 3 y 100 caracteres.";
    public const string PageOutOfRange = "La página debe ser mayor o igual que 1.";
    public const string PageSizeOutOfRange = "El tamaño de página debe estar entre 1 y 100.";
    public const string StatusInvalid = "El estado debe ser active, inactive o all.";
    public const string FieldValueInvalid = "El valor no es válido.";
}
