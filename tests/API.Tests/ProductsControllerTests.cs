using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using ProductsApi.Application.Common;
using ProductsApi.Application.DTOs.Auth;
using ProductsApi.Application.DTOs.Products;
using Xunit;

namespace ProductsApi.Api.Tests;

public class ProductsControllerTests : IClassFixture<ProductsApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductsControllerTests(ProductsApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var username = $"produser_{Guid.NewGuid():N}";
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest { Username = username, Email = $"{username}@test.com", Password = "Password1" });
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return _client;
    }

    [Fact]
    public async Task GetAll_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.GetAsync("/api/v1/products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ThenGetById_ReturnsCreatedProduct()
    {
        var client = await CreateAuthenticatedClientAsync();
        var createDto = new ProductCreateDto
        {
            ProductName = "Integration Test Widget",
            Items = new List<ItemCreateDto> { new() { Quantity = 4 } }
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/products", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();

        var getResponse = await client.GetAsync($"/api/v1/products/{created!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<ProductDto>();
        fetched!.ProductName.Should().Be("Integration Test Widget");
        fetched.Items.Should().ContainSingle(i => i.Quantity == 4);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenProductNameMissing()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/products", new ProductCreateDto { ProductName = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_ForUnknownId()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/v1/products/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ChangesProductName()
    {
        var client = await CreateAuthenticatedClientAsync();
        var created = await (await client.PostAsJsonAsync("/api/v1/products", new ProductCreateDto { ProductName = "Before" }))
            .Content.ReadFromJsonAsync<ProductDto>();

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/products/{created!.Id}", new ProductUpdateDto { ProductName = "After" });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ProductDto>();
        updated!.ProductName.Should().Be("After");
        updated.ModifiedOn.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_ReturnsForbidden_ForNonAdminUser()
    {
        var client = await CreateAuthenticatedClientAsync();
        var created = await (await client.PostAsJsonAsync("/api/v1/products", new ProductCreateDto { ProductName = "ToDelete" }))
            .Content.ReadFromJsonAsync<ProductDto>();

        var deleteResponse = await client.DeleteAsync($"/api/v1/products/{created!.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAll_ReturnsPagedResult()
    {
        var client = await CreateAuthenticatedClientAsync();
        await client.PostAsJsonAsync("/api/v1/products", new ProductCreateDto { ProductName = "PageTest1" });
        await client.PostAsJsonAsync("/api/v1/products", new ProductCreateDto { ProductName = "PageTest2" });

        var response = await client.GetAsync("/api/v1/products?pageNumber=1&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        result!.Items.Should().HaveCount(1);
        result.PageSize.Should().Be(1);
    }

    [Fact]
    public async Task AddItem_ThenGetItems_ReturnsAddedItem()
    {
        var client = await CreateAuthenticatedClientAsync();
        var created = await (await client.PostAsJsonAsync("/api/v1/products", new ProductCreateDto { ProductName = "WithItems" }))
            .Content.ReadFromJsonAsync<ProductDto>();

        var addItemResponse = await client.PostAsJsonAsync($"/api/v1/products/{created!.Id}/items", new ItemCreateDto { Quantity = 7 });
        addItemResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var itemsResponse = await client.GetAsync($"/api/v1/products/{created.Id}/items");
        var items = await itemsResponse.Content.ReadFromJsonAsync<List<ItemDto>>();
        items.Should().ContainSingle(i => i.Quantity == 7);
    }
}
