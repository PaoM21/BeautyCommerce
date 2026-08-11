using BeautyCommerce.Application.Features.Products.DTOs;
using MediatR;

namespace BeautyCommerce.Application.Features.Products.Commands.UpdateStock;

public class UpdateStockCommand : IRequest
{
    public UpdateStockDto Stock { get; set; } = new();
}
