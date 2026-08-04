namespace auth_api_login.Domain.Interfaces;

public interface ITokenBlacklistRepository
{
    Task RevokeAsync(string jti, DateTime expiresAt, CancellationToken cancellationToken = default);
    Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default);
}
