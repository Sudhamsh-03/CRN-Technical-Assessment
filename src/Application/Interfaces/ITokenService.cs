using ProductsApi.Domain.Entities;

namespace ProductsApi.Application.Interfaces;

public interface ITokenService
{
    (string token, DateTime expiresOn) GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
