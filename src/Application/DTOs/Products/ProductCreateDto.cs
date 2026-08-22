namespace ProductsApi.Application.DTOs.Products;

public class ProductCreateDto
{
    public string ProductName { get; set; } = string.Empty;
    public List<ItemCreateDto> Items { get; set; } = new();
}
