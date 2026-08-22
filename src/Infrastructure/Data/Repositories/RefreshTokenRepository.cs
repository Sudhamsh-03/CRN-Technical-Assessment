using Microsoft.EntityFrameworkCore;
using ProductsApi.Application.Interfaces.Repositories;
using ProductsApi.Domain.Entities;

namespace ProductsApi.Infrastructure.Data.Repositories;

public class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(r => r.Token == token, cancellationToken);
}
