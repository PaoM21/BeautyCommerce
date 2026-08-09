using BeautyCommerce.Application.Common.Exceptions;

namespace BeautyCommerce.Application.Common.Helpers;

public static class Guard
{
    public static void AgainstNull(object? entity, string message)
    {
        if (entity == null)
            throw new NotFoundException(message);
    }
}
