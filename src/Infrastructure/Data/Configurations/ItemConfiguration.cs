using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductsApi.Domain.Entities;

namespace ProductsApi.Infrastructure.Data.Configurations;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("Item");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Quantity).IsRequired();

        builder.HasIndex(i => i.ProductId);
    }
}
