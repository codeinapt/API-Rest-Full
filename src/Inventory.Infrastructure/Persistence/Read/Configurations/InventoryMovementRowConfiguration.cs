using Inventory.Infrastructure.Persistence.Read.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Read.Configurations;

internal sealed class InventoryMovementRowConfiguration : IEntityTypeConfiguration<InventoryMovementRow>
{
    public void Configure(EntityTypeBuilder<InventoryMovementRow> builder)
    {
        builder.ToTable("InventoryMovements");
        builder.HasKey(movement => movement.Id);
        builder.Property(movement => movement.Notes).HasMaxLength(500);
        builder.Property(movement => movement.CreatedBy).HasMaxLength(100);
    }
}
