using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductsApi.Application.Common;
using ProductsApi.Application.DTOs.Products;
using ProductsApi.Application.Interfaces;

namespace ProductsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/products")]
[Produces("application/json")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>Gets a paged list of products, optionally filtered by name.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetAll(
        [FromQuery] PaginationParams paginationParams, CancellationToken cancellationToken)
    {
        var result = await _productService.GetAllAsync(paginationParams, cancellationToken);
        return Ok(result);
    }

    /// <summary>Gets a single product, including its items, by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var product = await _productService.GetByIdAsync(id, cancellationToken);
        return Ok(product);
    }

    /// <summary>Creates a new product, optionally with initial items.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDto>> Create(ProductCreateDto dto, CancellationToken cancellationToken)
    {
        var createdBy = User.Identity?.Name ?? "unknown";
        var product = await _productService.CreateAsync(dto, createdBy, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = product.Id, version = "1.0" }, product);
    }

    /// <summary>Updates an existing product's details.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> Update(int id, ProductUpdateDto dto, CancellationToken cancellationToken)
    {
        var modifiedBy = User.Identity?.Name ?? "unknown";
        var product = await _productService.UpdateAsync(id, dto, modifiedBy, cancellationToken);
        return Ok(product);
    }

    /// <summary>Deletes a product. Requires the Admin role.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _productService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Gets the items belonging to a product.</summary>
    [HttpGet("{id:int}/items")]
    [ProducesResponseType(typeof(IReadOnlyList<ItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ItemDto>>> GetItems(int id, CancellationToken cancellationToken)
    {
        var items = await _productService.GetItemsAsync(id, cancellationToken);
        return Ok(items);
    }

    /// <summary>Adds a new item to a product.</summary>
    [HttpPost("{id:int}/items")]
    [ProducesResponseType(typeof(ItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemDto>> AddItem(int id, ItemCreateDto dto, CancellationToken cancellationToken)
    {
        var item = await _productService.AddItemAsync(id, dto, cancellationToken);
        return CreatedAtAction(nameof(GetItems), new { id, version = "1.0" }, item);
    }
}
