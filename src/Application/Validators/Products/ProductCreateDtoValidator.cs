using FluentValidation;
using ProductsApi.Application.DTOs.Products;

namespace ProductsApi.Application.Validators.Products;

public class ProductCreateDtoValidator : AbstractValidator<ProductCreateDto>
{
    public ProductCreateDtoValidator()
    {
        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(255).WithMessage("Product name must not exceed 255 characters.");

        RuleForEach(x => x.Items).SetValidator(new ItemCreateDtoValidator());
    }
}
