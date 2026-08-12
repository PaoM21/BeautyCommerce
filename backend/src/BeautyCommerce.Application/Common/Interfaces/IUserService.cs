namespace BeautyCommerce.Application.Common.Interfaces
{
    using BeautyCommerce.Application.Features.Users.DTOs;

    public interface IUserService
    {
        Task<int> GetTotalCustomersAsync();

        Task<List<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<List<UserDto>> GetCustomersAsync(CancellationToken cancellationToken = default);
    }
}
