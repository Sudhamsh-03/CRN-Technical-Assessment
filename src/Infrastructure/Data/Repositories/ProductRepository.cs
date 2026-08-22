using Microsoft.EntityFrameworkCore;
using ProductsApi.Application.Interfaces.Repositories;
using ProductsApi.Domain.Entities;

namespace ProductsApi.Infrastructure.Data.Repositories;

public class ProductRepository : GenericRepository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Product?> GetByIdWithItemsAsync(int id, CancellationToken cancellationToken = default)
        => await DbSet.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, string? searchTerm, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().Include(p => p.Items).AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(p => p.ProductName.Contains(searchTerm));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
