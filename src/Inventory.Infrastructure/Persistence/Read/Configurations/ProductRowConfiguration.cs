using Inventory.Infrastructure.Persistence.Read.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Read.Configurations;

internal sealed class ProductRowConfiguration : IEntityTypeConfiguration<ProductRow>
{
    public void Configure(EntityTypeBuilder<ProductRow> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Sku).HasMaxLength(50).IsUnicode(false);
        builder.Property(product => product.Name).HasMaxLength(150);
        builder.Property(product => product.Description).HasMaxLength(1000);
        builder.Property(product => product.DeactivationNotes).HasMaxLength(500);
        builder.Property(product => product.DeactivatedBy).HasMaxLength(100);
        builder.Property(product => product.ReactivatedBy).HasMaxLength(100);
    }
}
