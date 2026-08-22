using ProductsApi.Application.Interfaces;
using ProductsApi.Application.Interfaces.Repositories;
using ProductsApi.Infrastructure.Data.Repositories;

namespace ProductsApi.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    private IProductRepository? _products;
    private IItemRepository? _items;
    private IUserRepository? _users;
    private IRefreshTokenRepository? _refreshTokens;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IProductRepository Products => _products ??= new ProductRepository(_context);
    public IItemRepository Items => _items ??= new ItemRepository(_context);
    public IUserRepository Users => _users ??= new UserRepository(_context);
    public IRefreshTokenRepository RefreshTokens => _refreshTokens ??= new RefreshTokenRepository(_context);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
