using ProductsApi.Application.Common;
using ProductsApi.Application.DTOs.Products;
using ProductsApi.Application.Interfaces;
using ProductsApi.Application.Mapping;
using ProductsApi.Domain.Entities;
using ProductsApi.Domain.Exceptions;

namespace ProductsApi.Application.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<ProductDto>> GetAllAsync(PaginationParams paginationParams, CancellationToken cancellationToken = default)
    {
        var (products, totalCount) = await _unitOfWork.Products.GetPagedAsync(
            paginationParams.PageNumber, paginationParams.PageSize, paginationParams.SearchTerm, cancellationToken);

        return new PagedResult<ProductDto>
        {
            Items = products.ToDto(),
            PageNumber = paginationParams.PageNumber,
            PageSize = paginationParams.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ProductDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdWithItemsAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), id);

        return product.ToDto();
    }

    public async Task<ProductDto> CreateAsync(ProductCreateDto dto, string createdBy, CancellationToken cancellationToken = default)
    {
        var product = new Product
        {
            ProductName = dto.ProductName,
            CreatedBy = createdBy,
            CreatedOn = DateTime.UtcNow,
            Items = dto.Items.Select(i => new Item { Quantity = i.Quantity }).ToList()
        };

        await _unitOfWork.Products.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.ToDto();
    }

    public async Task<ProductDto> UpdateAsync(int id, ProductUpdateDto dto, string modifiedBy, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), id);

        product.ProductName = dto.ProductName;
        product.ModifiedBy = modifiedBy;
        product.ModifiedOn = DateTime.UtcNow;

        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), id);

        _unitOfWork.Products.Remove(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ItemDto>> GetItemsAsync(int productId, CancellationToken cancellationToken = default)
    {
        var exists = await _unitOfWork.Products.ExistsAsync(p => p.Id == productId, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException(nameof(Product), productId);
        }

        var items = await _unitOfWork.Items.GetByProductIdAsync(productId, cancellationToken);

        return items.ToDto();
    }

    public async Task<ItemDto> AddItemAsync(int productId, ItemCreateDto dto, CancellationToken cancellationToken = default)
    {
        var exists = await _unitOfWork.Products.ExistsAsync(p => p.Id == productId, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException(nameof(Product), productId);
        }

        var item = new Item { ProductId = productId, Quantity = dto.Quantity };

        await _unitOfWork.Items.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return item.ToDto();
    }
}
