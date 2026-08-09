using BeautyCommerce.Application.Features.ProductVariants.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.ProductVariants.Commands.UpdateStock;

public class UpdateStockCommand : IRequest
{
    public UpdateStockDto Stock { get; set; } = new();
}
