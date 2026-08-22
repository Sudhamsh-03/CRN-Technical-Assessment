using ProductsApi.Application.DTOs.Auth;
using ProductsApi.Application.Interfaces;
using ProductsApi.Domain.Entities;
using ProductsApi.Domain.Enums;
using ProductsApi.Domain.Exceptions;

namespace ProductsApi.Application.Services;

public class AuthService : IAuthService
{
    private const int RefreshTokenLifetimeDays = 7;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(IUnitOfWork unitOfWork, ITokenService tokenService, IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _unitOfWork.Users.GetByUsernameAsync(request.Username, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException($"Username '{request.Username}' is already taken.");
        }

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.User,
            CreatedOn = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByUsernameAsync(request.Username, cancellationToken);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new AuthenticationException("Invalid username or password.");
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var storedToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(refreshToken, cancellationToken);
        if (storedToken is null || !storedToken.IsActive)
        {
            throw new AuthenticationException("Invalid or expired refresh token.");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(storedToken.UserId, cancellationToken)
            ?? throw new AuthenticationException("User no longer exists.");

        storedToken.RevokedOn = DateTime.UtcNow;

        var response = await IssueTokensAsync(user, cancellationToken);
        storedToken.ReplacedByToken = response.RefreshToken;

        _unitOfWork.RefreshTokens.Update(storedToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var storedToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(refreshToken, cancellationToken);
        if (storedToken is null || !storedToken.IsActive)
        {
            throw new AuthenticationException("Invalid or expired refresh token.");
        }

        storedToken.RevokedOn = DateTime.UtcNow;
        _unitOfWork.RefreshTokens.Update(storedToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var (accessToken, expiresOn) = _tokenService.GenerateAccessToken(user);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            CreatedOn = DateTime.UtcNow,
            ExpiresOn = DateTime.UtcNow.AddDays(RefreshTokenLifetimeDays)
        };

        await _unitOfWork.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            Username = user.Username,
            Role = user.Role.ToString(),
            AccessToken = accessToken,
            AccessTokenExpiresOn = expiresOn,
            RefreshToken = refreshTokenValue
        };
    }
}
