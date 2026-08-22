using ProductsApi.Application.Interfaces.Repositories;

namespace ProductsApi.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IProductRepository Products { get; }
    IItemRepository Items { get; }
    IUserRepository Users { get; }
    IRefreshTokenRepository RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
