using FluentValidation;
using ProductsApi.Application.DTOs.Products;

namespace ProductsApi.Application.Validators.Products;

public class ItemCreateDtoValidator : AbstractValidator<ItemCreateDto>
{
    public ItemCreateDtoValidator()
    {
        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0).WithMessage("Quantity cannot be negative.");
    }
}
