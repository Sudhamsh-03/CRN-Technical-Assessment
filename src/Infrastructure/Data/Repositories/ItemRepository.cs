using Microsoft.EntityFrameworkCore;
using ProductsApi.Application.Interfaces.Repositories;
using ProductsApi.Domain.Entities;

namespace ProductsApi.Infrastructure.Data.Repositories;

public class ItemRepository : GenericRepository<Item>, IItemRepository
{
    public ItemRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Item>> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking().Where(i => i.ProductId == productId).ToListAsync(cancellationToken);
}
