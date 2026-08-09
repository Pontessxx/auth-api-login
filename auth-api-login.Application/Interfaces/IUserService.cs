namespace auth_api_login.Application.Interfaces;

public interface IUserService
{
    Task<Result<UserResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<UserResponse>> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
