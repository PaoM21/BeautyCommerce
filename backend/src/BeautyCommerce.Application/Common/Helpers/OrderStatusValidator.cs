using BeautyCommerce.Domain.Enums;

namespace BeautyCommerce.Application.Common.Helpers;

public static class OrderStatusValidator
{
    public static bool IsValidTransition(
        OrderStatus current,
        OrderStatus next)
    {
        return current switch
        {
            OrderStatus.Pending =>
                next == OrderStatus.Paid ||
                next == OrderStatus.Cancelled,

            OrderStatus.Paid =>
                next == OrderStatus.Processing ||
                next == OrderStatus.Cancelled,

            OrderStatus.Processing =>
                next == OrderStatus.Shipped,

            OrderStatus.Shipped =>
                next == OrderStatus.Delivered,

            _ => false
        };
    }
}
