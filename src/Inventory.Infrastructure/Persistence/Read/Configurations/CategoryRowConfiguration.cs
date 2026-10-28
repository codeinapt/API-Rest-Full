using Inventory.Infrastructure.Persistence.Read.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Read.Configurations;

internal sealed class CategoryRowConfiguration : IEntityTypeConfiguration<CategoryRow>
{
    public void Configure(EntityTypeBuilder<CategoryRow> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Name).HasMaxLength(100);
    }
}
