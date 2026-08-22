using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProductsApi.Application.DTOs.Auth;
using Xunit;

namespace ProductsApi.Api.Tests;

public class AuthControllerTests : IClassFixture<ProductsApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(ProductsApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ReturnsCreated_WithTokens()
    {
        var request = new RegisterRequest { Username = $"user_{Guid.NewGuid():N}", Email = "reg@test.com", Password = "Password1" };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_ReturnsConflict_WhenUsernameAlreadyExists()
    {
        var username = $"dupe_{Guid.NewGuid():N}";
        var request = new RegisterRequest { Username = username, Email = "dupe@test.com", Password = "Password1" };
        await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WithBadCredentials()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest { Username = "nobody", Password = "wrong" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ReturnsTokens_WithValidCredentials()
    {
        var username = $"loginuser_{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest { Username = username, Email = "login@test.com", Password = "Password1" });

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest { Username = username, Password = "Password1" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RefreshAndRevoke_RoundTrip_Succeeds()
    {
        var username = $"refreshuser_{Guid.NewGuid():N}";
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest { Username = username, Email = "refresh@test.com", Password = "Password1" });
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var refreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest { RefreshToken = auth!.RefreshToken });
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var newAuth = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var revokeResponse = await _client.PostAsJsonAsync("/api/v1/auth/revoke", new RefreshTokenRequest { RefreshToken = newAuth!.RefreshToken });
        revokeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var reuseResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest { RefreshToken = auth.RefreshToken });
        reuseResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
