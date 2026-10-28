using System.Text.RegularExpressions;
using Inventory.Domain.Categories;
using Inventory.Domain.Common;

namespace Inventory.Domain.Products;

public sealed partial class Product
{
    public const int SkuMinimumLength = 3;

    public const int SkuMaximumLength = 50;

    public const string SkuPattern = @"^[A-Z0-9-]+\z";

    public const int NameMinimumLength = 3;

    public const int NameMaximumLength = 150;

    public const int DescriptionMaximumLength = 1000;

    public const int DeactivationNotesMaximumLength = 500;

    private Product()
    {
    }

    public int Id { get; private set; }

    public string Sku { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public int CategoryId { get; private set; }

    public int Stock { get; private set; }

    public int MinimumStock { get; private set; }

    public bool IsActive { get; private set; }

    public DeactivationReason? DeactivationReason { get; private set; }

    public string? DeactivationNotes { get; private set; }

    public DateTime? DeactivatedAt { get; private set; }

    public string? DeactivatedBy { get; private set; }

    public DateTime? ReactivatedAt { get; private set; }

    public string? ReactivatedBy { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public static Product Create(Category category, string sku, ProductDetails details, DateTime createdAt)
    {
        EnsureValidSku(sku);
        EnsureValidDetails(details);
        EnsureActiveReferencedCategory(category);
        var product = new Product { Sku = sku, CategoryId = category.Id, IsActive = true, CreatedAt = createdAt };
        product.ApplyDetails(details);

        return product;
    }

    public void UpdateDetails(ProductDetails details, DateTime updatedAt)
    {
        EnsureActive();
        EnsureValidDetails(details);
        ApplyDetails(details);
        UpdatedAt = updatedAt;
    }

    public void ChangeCategory(Category category, DateTime updatedAt)
    {
        EnsureActive();
        EnsureActiveReferencedCategory(category);
        CategoryId = category.Id;
        UpdatedAt = updatedAt;
    }

    public void Delete(DateTime deletedAt, string deletedBy)
    {
        EnsureActive();
        EnsureHasNoStock();
        ApplyDeactivation(new ProductDeactivation(Products.DeactivationReason.Discontinued, null, deletedAt, deletedBy));
    }

    public void Deactivate(ProductDeactivation deactivation)
    {
        EnsureActive();
        EnsureValidDeactivation(deactivation);
        ApplyDeactivation(deactivation);
    }

    public void Reactivate(Category category, DateTime reactivatedAt, string reactivatedBy)
    {
        EnsureInactive();
        EnsureActiveOwnCategory(category);
        IsActive = true;
        ReactivatedAt = reactivatedAt;
        ReactivatedBy = reactivatedBy;
    }

    public bool IsLowStock()
    {
        return Stock <= MinimumStock;
    }

    private static void EnsureValidSku(string sku)
    {
        var hasValidLength = sku.Length is >= SkuMinimumLength and <= SkuMaximumLength;
        if (!hasValidLength || !SkuRegex().IsMatch(sku))
        {
            throw new DomainException(ErrorCode.ValidationError);
        }
    }

    private static void EnsureValidDetails(ProductDetails details)
    {
        var hasValidNameLength = details.Name.Length is >= NameMinimumLength and <= NameMaximumLength;
        var hasValidName = hasValidNameLength && !string.IsNullOrWhiteSpace(details.Name);
        var hasValidDescription = details.Description is null || details.Description.Length <= DescriptionMaximumLength;
        if (!hasValidName || !hasValidDescription || details.MinimumStock < 0)
        {
            throw new DomainException(ErrorCode.ValidationError);
        }
    }

    private static void EnsureValidDeactivation(ProductDeactivation deactivation)
    {
        var hasValidNotesLength = deactivation.Notes is null || deactivation.Notes.Length <= DeactivationNotesMaximumLength;
        var isMissingRequiredNotes = deactivation.Reason == Products.DeactivationReason.Other && string.IsNullOrWhiteSpace(deactivation.Notes);
        if (!Enum.IsDefined(deactivation.Reason) || !hasValidNotesLength || isMissingRequiredNotes)
        {
            throw new DomainException(ErrorCode.ValidationError);
        }
    }

    private static void EnsureActiveReferencedCategory(Category category)
    {
        if (!category.IsActive)
        {
            throw new DomainException(ErrorCode.ReferencedCategoryInvalid, category.Id);
        }
    }

    private static void EnsureActiveOwnCategory(Category category)
    {
        if (!category.IsActive)
        {
            throw new DomainException(ErrorCode.CategoryInactive, category.Id);
        }
    }

    [GeneratedRegex(SkuPattern)]
    private static partial Regex SkuRegex();

    private void EnsureActive()
    {
        if (!IsActive)
        {
            throw new DomainException(ErrorCode.ProductInactive, Id);
        }
    }

    private void EnsureInactive()
    {
        if (IsActive)
        {
            throw new DomainException(ErrorCode.ProductAlreadyActive, Id);
        }
    }

    private void EnsureHasNoStock()
    {
        if (Stock > 0)
        {
            throw new DomainException(ErrorCode.ProductHasStock, Stock);
        }
    }

    private void ApplyDetails(ProductDetails details)
    {
        Name = details.Name;
        Description = details.Description;
        MinimumStock = details.MinimumStock;
    }

    private void ApplyDeactivation(ProductDeactivation deactivation)
    {
        IsActive = false;
        DeactivationReason = deactivation.Reason;
        DeactivationNotes = deactivation.Notes;
        DeactivatedAt = deactivation.At;
        DeactivatedBy = deactivation.By;
    }
}
