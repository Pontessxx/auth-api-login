namespace auth_api_login.Infrastructure.Repositories;

public class TokenBlacklistRepository : ITokenBlacklistRepository
{
    private readonly AppDbContext _dbContext;

    public TokenBlacklistRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RevokeAsync(string jti, DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        var alreadyRevoked = await _dbContext.RevokedTokens.AnyAsync(t => t.Jti == jti, cancellationToken);
        if (alreadyRevoked)
        {
            return;
        }

        await _dbContext.RevokedTokens.AddAsync(new RevokedToken
        {
            Id = Guid.CreateVersion7(),
            Jti = jti,
            ExpiresAt = expiresAt,
            RevokedAt = DateTime.UtcNow
        }, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default)
    {
        return _dbContext.RevokedTokens.AnyAsync(t => t.Jti == jti, cancellationToken);
    }
}
