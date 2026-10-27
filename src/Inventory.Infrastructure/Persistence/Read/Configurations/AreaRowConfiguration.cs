using Inventory.Infrastructure.Persistence.Read.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Read.Configurations;

internal sealed class AreaRowConfiguration : IEntityTypeConfiguration<AreaRow>
{
    public void Configure(EntityTypeBuilder<AreaRow> builder)
    {
        builder.ToTable("Areas");
        builder.HasKey(area => area.Id);
        builder.Property(area => area.Code).HasMaxLength(10).IsUnicode(false);
        builder.Property(area => area.Name).HasMaxLength(100);
    }
}
