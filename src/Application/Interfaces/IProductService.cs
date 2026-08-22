using ProductsApi.Application.Common;
using ProductsApi.Application.DTOs.Products;

namespace ProductsApi.Application.Interfaces;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetAllAsync(PaginationParams paginationParams, CancellationToken cancellationToken = default);
    Task<ProductDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateAsync(ProductCreateDto dto, string createdBy, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateAsync(int id, ProductUpdateDto dto, string modifiedBy, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ItemDto>> GetItemsAsync(int productId, CancellationToken cancellationToken = default);
    Task<ItemDto> AddItemAsync(int productId, ItemCreateDto dto, CancellationToken cancellationToken = default);
}
