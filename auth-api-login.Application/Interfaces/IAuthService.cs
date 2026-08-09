namespace auth_api_login.Application.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponse>> 
    RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default);
    Task LogoutAsync(Guid userId, string jti, DateTime expiresAt, CancellationToken cancellationToken = default);
}
