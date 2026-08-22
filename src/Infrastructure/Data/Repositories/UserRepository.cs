using Microsoft.EntityFrameworkCore;
using ProductsApi.Application.Interfaces.Repositories;
using ProductsApi.Domain.Entities;

namespace ProductsApi.Infrastructure.Data.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
}
