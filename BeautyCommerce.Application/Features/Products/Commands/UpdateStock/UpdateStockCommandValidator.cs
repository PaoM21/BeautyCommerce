using FluentValidation;

namespace BeautyCommerce.Application.Features.Products.Commands.UpdateStock;

public class UpdateStockCommandValidator
    : AbstractValidator<UpdateStockCommand>
{
    public UpdateStockCommandValidator()
    {
        RuleFor(x => x.Stock.ProductVariantId)
            .NotEmpty();

        RuleFor(x => x.Stock.Stock)
            .GreaterThanOrEqualTo(0);
    }
}
