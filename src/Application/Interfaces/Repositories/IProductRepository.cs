using ProductsApi.Domain.Entities;

namespace ProductsApi.Application.Interfaces.Repositories;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<Product?> GetByIdWithItemsAsync(int id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, string? searchTerm, CancellationToken cancellationToken = default);
}
