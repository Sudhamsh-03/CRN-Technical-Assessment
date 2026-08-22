using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProductsApi.Domain.Entities;
using ProductsApi.Infrastructure.Data;
using ProductsApi.Infrastructure.Data.Repositories;
using Xunit;

namespace ProductsApi.Infrastructure.Tests.Data;

public class ProductRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ProductRepository _sut;

    public ProductRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _sut = new ProductRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ThenSave_PersistsProduct()
    {
        var product = new Product { ProductName = "Widget", CreatedBy = "alice", CreatedOn = DateTime.UtcNow };

        await _sut.AddAsync(product);
        await _context.SaveChangesAsync();

        (await _context.Products.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task GetByIdWithItemsAsync_ReturnsProductWithItems()
    {
        var product = new Product
        {
            ProductName = "Widget",
            CreatedBy = "alice",
            CreatedOn = DateTime.UtcNow,
            Items = new List<Item> { new() { Quantity = 5 }, new() { Quantity = 10 } }
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var result = await _sut.GetByIdWithItemsAsync(product.Id);

        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersBySearchTerm_AndPaginatesResults()
    {
        for (var i = 1; i <= 15; i++)
        {
            _context.Products.Add(new Product
            {
                ProductName = i % 2 == 0 ? $"Gadget {i}" : $"Widget {i}",
                CreatedBy = "alice",
                CreatedOn = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();

        var (items, totalCount) = await _sut.GetPagedAsync(pageNumber: 1, pageSize: 5, searchTerm: "Widget");

        totalCount.Should().Be(8);
        items.Should().HaveCount(5);
        items.Should().OnlyContain(p => p.ProductName.Contains("Widget"));
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_WhenProductDoesNotExist()
    {
        var exists = await _sut.ExistsAsync(p => p.Id == 999);

        exists.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
