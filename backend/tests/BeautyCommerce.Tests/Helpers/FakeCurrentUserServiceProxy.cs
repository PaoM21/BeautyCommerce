using BeautyCommerce.Application.Common.Interfaces;

namespace BeautyCommerce.Tests.Helpers;

public class FakeCurrentUserServiceProxy : ICurrentUserService
{
    private readonly Guid _userId;

    public FakeCurrentUserServiceProxy(Guid userId)
    {
        _userId = userId;
    }

    public Guid? UserId => _userId;

    public string? Email => "test@test.com";

    public bool IsAuthenticated => true;
}
