using BeautyCommerce.Application.Common.Interfaces;

namespace BeautyCommerce.Tests.Helpers;

public class FakeCurrentUserService : ICurrentUserService
{
    public Guid? UserId => Guid.NewGuid();

    public string? Email => "test@test.com";

    public bool IsAuthenticated => true;
}