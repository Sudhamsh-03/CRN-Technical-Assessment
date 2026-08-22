using FluentAssertions;
using Moq;
using ProductsApi.Application.Common;
using ProductsApi.Application.DTOs.Products;
using ProductsApi.Application.Interfaces;
using ProductsApi.Application.Interfaces.Repositories;
using ProductsApi.Application.Services;
using ProductsApi.Domain.Entities;
using ProductsApi.Domain.Exceptions;
using Xunit;

namespace ProductsApi.Application.Tests.Services;

public class ProductServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly Mock<IItemRepository> _itemRepositoryMock = new();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Items).Returns(_itemRepositoryMock.Object);
        _sut = new ProductService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResult_WithMappedItems()
    {
        var products = new List<Product>
        {
            new() { Id = 1, ProductName = "Widget", CreatedBy = "alice", CreatedOn = DateTime.UtcNow }
        };
        _productRepositoryMock
            .Setup(r => r.GetPagedAsync(1, 10, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((products, 1));

        var result = await _sut.GetAllAsync(new PaginationParams { PageNumber = 1, PageSize = 10 });

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(p => p.ProductName == "Widget");
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFoundException_WhenProductDoesNotExist()
    {
        _productRepositoryMock
            .Setup(r => r.GetByIdWithItemsAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var act = () => _sut.GetByIdAsync(99);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsProduct_WhenFound()
    {
        var product = new Product { Id = 5, ProductName = "Gadget", CreatedBy = "bob", CreatedOn = DateTime.UtcNow };
        _productRepositoryMock
            .Setup(r => r.GetByIdWithItemsAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _sut.GetByIdAsync(5);

        result.Id.Should().Be(5);
        result.ProductName.Should().Be("Gadget");
    }

    [Fact]
    public async Task CreateAsync_AddsProduct_AndReturnsMappedDto()
    {
        var dto = new ProductCreateDto
        {
            ProductName = "New Product",
            Items = new List<ItemCreateDto> { new() { Quantity = 3 } }
        };

        Product? capturedProduct = null;
        _productRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => capturedProduct = p)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(dto, "charlie");

        capturedProduct.Should().NotBeNull();
        capturedProduct!.CreatedBy.Should().Be("charlie");
        capturedProduct.Items.Should().ContainSingle(i => i.Quantity == 3);
        result.ProductName.Should().Be("New Product");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsNotFoundException_WhenProductDoesNotExist()
    {
        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var act = () => _sut.UpdateAsync(1, new ProductUpdateDto { ProductName = "X" }, "dave");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFields_WhenProductExists()
    {
        var product = new Product { Id = 1, ProductName = "Old", CreatedBy = "alice", CreatedOn = DateTime.UtcNow };
        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _sut.UpdateAsync(1, new ProductUpdateDto { ProductName = "Updated" }, "dave");

        result.ProductName.Should().Be("Updated");
        product.ModifiedBy.Should().Be("dave");
        product.ModifiedOn.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_RemovesProduct_WhenFound()
    {
        var product = new Product { Id = 7, ProductName = "ToDelete", CreatedBy = "alice", CreatedOn = DateTime.UtcNow };
        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        await _sut.DeleteAsync(7);

        _productRepositoryMock.Verify(r => r.Remove(product), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddItemAsync_ThrowsNotFoundException_WhenProductDoesNotExist()
    {
        _productRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => _sut.AddItemAsync(1, new ItemCreateDto { Quantity = 5 });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AddItemAsync_AddsItem_WhenProductExists()
    {
        _productRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.AddItemAsync(1, new ItemCreateDto { Quantity = 5 });

        result.Quantity.Should().Be(5);
        result.ProductId.Should().Be(1);
        _itemRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Item>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
