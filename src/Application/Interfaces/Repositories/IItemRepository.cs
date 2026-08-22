using ProductsApi.Domain.Entities;

namespace ProductsApi.Application.Interfaces.Repositories;

public interface IItemRepository : IGenericRepository<Item>
{
    Task<IReadOnlyList<Item>> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default);
}
