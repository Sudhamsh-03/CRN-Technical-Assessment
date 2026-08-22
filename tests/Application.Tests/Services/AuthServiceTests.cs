using FluentAssertions;
using Moq;
using ProductsApi.Application.DTOs.Auth;
using ProductsApi.Application.Interfaces;
using ProductsApi.Application.Interfaces.Repositories;
using ProductsApi.Application.Services;
using ProductsApi.Domain.Entities;
using ProductsApi.Domain.Enums;
using ProductsApi.Domain.Exceptions;
using Xunit;
using AuthenticationException = ProductsApi.Domain.Exceptions.AuthenticationException;

namespace ProductsApi.Application.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.RefreshTokens).Returns(_refreshTokenRepositoryMock.Object);
        _tokenServiceMock
            .Setup(t => t.GenerateAccessToken(It.IsAny<User>()))
            .Returns(("access-token", DateTime.UtcNow.AddMinutes(15)));
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("refresh-token");

        _sut = new AuthService(_unitOfWorkMock.Object, _tokenServiceMock.Object, _passwordHasherMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ThrowsConflictException_WhenUsernameTaken()
    {
        _userRepositoryMock
            .Setup(r => r.GetByUsernameAsync("existing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Username = "existing" });

        var act = () => _sut.RegisterAsync(new RegisterRequest { Username = "existing", Email = "a@b.com", Password = "Password1" });

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task RegisterAsync_CreatesUser_AndReturnsTokens()
    {
        _userRepositoryMock
            .Setup(r => r.GetByUsernameAsync("newuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordHasherMock.Setup(p => p.Hash("Password1")).Returns("hashed");

        var result = await _sut.RegisterAsync(new RegisterRequest { Username = "newuser", Email = "a@b.com", Password = "Password1" });

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        _userRepositoryMock.Verify(r => r.AddAsync(It.Is<User>(u => u.Username == "newuser" && u.Role == UserRole.User), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ThrowsAuthenticationException_WhenUserNotFound()
    {
        _userRepositoryMock
            .Setup(r => r.GetByUsernameAsync("ghost", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = () => _sut.LoginAsync(new LoginRequest { Username = "ghost", Password = "whatever" });

        await act.Should().ThrowAsync<AuthenticationException>();
    }

    [Fact]
    public async Task LoginAsync_ThrowsAuthenticationException_WhenPasswordInvalid()
    {
        var user = new User { Username = "bob", PasswordHash = "hashed" };
        _userRepositoryMock.Setup(r => r.GetByUsernameAsync("bob", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasherMock.Setup(p => p.Verify("wrong", "hashed")).Returns(false);

        var act = () => _sut.LoginAsync(new LoginRequest { Username = "bob", Password = "wrong" });

        await act.Should().ThrowAsync<AuthenticationException>();
    }

    [Fact]
    public async Task LoginAsync_ReturnsTokens_WhenCredentialsValid()
    {
        var user = new User { Id = 1, Username = "bob", PasswordHash = "hashed", Role = UserRole.User };
        _userRepositoryMock.Setup(r => r.GetByUsernameAsync("bob", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasherMock.Setup(p => p.Verify("correct", "hashed")).Returns(true);

        var result = await _sut.LoginAsync(new LoginRequest { Username = "bob", Password = "correct" });

        result.Username.Should().Be("bob");
        result.AccessToken.Should().Be("access-token");
    }

    [Fact]
    public async Task RefreshTokenAsync_ThrowsAuthenticationException_WhenTokenInactive()
    {
        _refreshTokenRepositoryMock
            .Setup(r => r.GetByTokenAsync("expired", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshToken { Token = "expired", ExpiresOn = DateTime.UtcNow.AddDays(-1) });

        var act = () => _sut.RefreshTokenAsync("expired");

        await act.Should().ThrowAsync<AuthenticationException>();
    }

    [Fact]
    public async Task RefreshTokenAsync_RotatesToken_WhenValid()
    {
        var storedToken = new RefreshToken { UserId = 1, Token = "valid", ExpiresOn = DateTime.UtcNow.AddDays(1) };
        _refreshTokenRepositoryMock
            .Setup(r => r.GetByTokenAsync("valid", It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 1, Username = "bob", Role = UserRole.User });

        var result = await _sut.RefreshTokenAsync("valid");

        storedToken.RevokedOn.Should().NotBeNull();
        storedToken.ReplacedByToken.Should().Be("refresh-token");
        result.RefreshToken.Should().Be("refresh-token");
    }
}
