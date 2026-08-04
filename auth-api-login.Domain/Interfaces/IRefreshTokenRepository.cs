namespace auth_api_login.Domain.Interfaces;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<int> CountActiveAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetOldestActiveAsync(Guid userId, CancellationToken cancellationToken = default);
    Task RevokeAsync(RefreshToken refreshToken, Guid? replacedByTokenId = null, CancellationToken cancellationToken = default);
    Task RevokeAllActiveAsync(Guid userId, CancellationToken cancellationToken = default);
}
