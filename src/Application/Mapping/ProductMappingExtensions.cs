using ProductsApi.Application.DTOs.Products;
using ProductsApi.Domain.Entities;

namespace ProductsApi.Application.Mapping;

public static class ProductMappingExtensions
{
    public static ProductDto ToDto(this Product product) => new()
    {
        Id = product.Id,
        ProductName = product.ProductName,
        CreatedBy = product.CreatedBy,
        CreatedOn = product.CreatedOn,
        ModifiedBy = product.ModifiedBy,
        ModifiedOn = product.ModifiedOn,
        Items = product.Items.Select(i => i.ToDto()).ToList()
    };

    public static List<ProductDto> ToDto(this IEnumerable<Product> products) =>
        products.Select(p => p.ToDto()).ToList();

    public static ItemDto ToDto(this Item item) => new()
    {
        Id = item.Id,
        ProductId = item.ProductId,
        Quantity = item.Quantity
    };

    public static List<ItemDto> ToDto(this IEnumerable<Item> items) =>
        items.Select(i => i.ToDto()).ToList();
}
