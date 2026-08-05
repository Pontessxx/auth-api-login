namespace auth_api_login.Tests.Infrastructure.Repositories;

public class RefreshTokenRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsToken()
    {
        using var dbContext = TestDbContextFactory.Create();
        var repository = new RefreshTokenRepository(dbContext);
        var token = new RefreshToken { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), TokenHash = "hash", ExpiresAt = DateTime.UtcNow.AddDays(7) };

        await repository.AddAsync(token);

        Assert.Equal(1, dbContext.RefreshTokens.Count());
    }

    [Fact]
    public async Task GetByHashAsync_WhenExists_ReturnsToken()
    {
        using var dbContext = TestDbContextFactory.Create();
        var token = new RefreshToken { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), TokenHash = "hash-value", ExpiresAt = DateTime.UtcNow.AddDays(7) };
        dbContext.RefreshTokens.Add(token);
        await dbContext.SaveChangesAsync();

        var repository = new RefreshTokenRepository(dbContext);
        var found = await repository.GetByHashAsync("hash-value");

        Assert.NotNull(found);
        Assert.Equal(token.Id, found!.Id);
    }

    [Fact]
    public async Task GetByHashAsync_WhenMissing_ReturnsNull()
    {
        using var dbContext = TestDbContextFactory.Create();
        var repository = new RefreshTokenRepository(dbContext);

        var found = await repository.GetByHashAsync("missing-hash");

        Assert.Null(found);
    }

    [Fact]
    public async Task CountActiveAsync_CountsOnlyNonRevokedNonExpiredTokensForUser()
    {
        using var dbContext = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        dbContext.RefreshTokens.AddRange(
            new RefreshToken { Id = Guid.NewGuid(), UserId = userId, TokenHash = "a", ExpiresAt = DateTime.UtcNow.AddDays(1) },
            new RefreshToken { Id = Guid.NewGuid(), UserId = userId, TokenHash = "b", ExpiresAt = DateTime.UtcNow.AddDays(1), RevokedAt = DateTime.UtcNow },
            new RefreshToken { Id = Guid.NewGuid(), UserId = userId, TokenHash = "c", ExpiresAt = DateTime.UtcNow.AddDays(-1) },
            new RefreshToken { Id = Guid.NewGuid(), UserId = otherUserId, TokenHash = "d", ExpiresAt = DateTime.UtcNow.AddDays(1) });
        await dbContext.SaveChangesAsync();

        var repository = new RefreshTokenRepository(dbContext);
        var count = await repository.CountActiveAsync(userId);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetOldestActiveAsync_ReturnsOldestByCreatedAt()
    {
        using var dbContext = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var older = new RefreshToken { Id = Guid.NewGuid(), UserId = userId, TokenHash = "old", ExpiresAt = DateTime.UtcNow.AddDays(1), CreatedAt = DateTime.UtcNow.AddDays(-2) };
        var newer = new RefreshToken { Id = Guid.NewGuid(), UserId = userId, TokenHash = "new", ExpiresAt = DateTime.UtcNow.AddDays(1), CreatedAt = DateTime.UtcNow.AddDays(-1) };
        dbContext.RefreshTokens.AddRange(older, newer);
        await dbContext.SaveChangesAsync();

        var repository = new RefreshTokenRepository(dbContext);
        var oldest = await repository.GetOldestActiveAsync(userId);

        Assert.NotNull(oldest);
        Assert.Equal(older.Id, oldest!.Id);
    }

    [Fact]
    public async Task GetOldestActiveAsync_WhenNoneActive_ReturnsNull()
    {
        using var dbContext = TestDbContextFactory.Create();
        var repository = new RefreshTokenRepository(dbContext);

        var oldest = await repository.GetOldestActiveAsync(Guid.NewGuid());

        Assert.Null(oldest);
    }

    [Fact]
    public async Task RevokeAsync_SetsRevokedAtAndReplacedByTokenId()
    {
        using var dbContext = TestDbContextFactory.Create();
        var token = new RefreshToken { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), TokenHash = "hash", ExpiresAt = DateTime.UtcNow.AddDays(1) };
        dbContext.RefreshTokens.Add(token);
        await dbContext.SaveChangesAsync();

        var replacedById = Guid.NewGuid();
        var repository = new RefreshTokenRepository(dbContext);
        await repository.RevokeAsync(token, replacedById);

        Assert.NotNull(token.RevokedAt);
        Assert.Equal(replacedById, token.ReplacedByTokenId);
    }

    [Fact]
    public async Task RevokeAsync_WithoutReplacement_LeavesReplacedByTokenIdNull()
    {
        using var dbContext = TestDbContextFactory.Create();
        var token = new RefreshToken { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), TokenHash = "hash", ExpiresAt = DateTime.UtcNow.AddDays(1) };
        dbContext.RefreshTokens.Add(token);
        await dbContext.SaveChangesAsync();

        var repository = new RefreshTokenRepository(dbContext);
        await repository.RevokeAsync(token);

        Assert.NotNull(token.RevokedAt);
        Assert.Null(token.ReplacedByTokenId);
    }

    [Fact]
    public async Task RevokeAllActiveAsync_RevokesOnlyActiveTokensForUser()
    {
        using var dbContext = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var active = new RefreshToken { Id = Guid.NewGuid(), UserId = userId, TokenHash = "a", ExpiresAt = DateTime.UtcNow.AddDays(1) };
        var originalRevokedAt = DateTime.UtcNow.AddMinutes(-5);
        var alreadyRevoked = new RefreshToken { Id = Guid.NewGuid(), UserId = userId, TokenHash = "b", ExpiresAt = DateTime.UtcNow.AddDays(1), RevokedAt = originalRevokedAt };
        var expired = new RefreshToken { Id = Guid.NewGuid(), UserId = userId, TokenHash = "c", ExpiresAt = DateTime.UtcNow.AddDays(-1) };
        dbContext.RefreshTokens.AddRange(active, alreadyRevoked, expired);
        await dbContext.SaveChangesAsync();

        var repository = new RefreshTokenRepository(dbContext);
        await repository.RevokeAllActiveAsync(userId);

        Assert.NotNull(active.RevokedAt);
        Assert.Equal(originalRevokedAt, alreadyRevoked.RevokedAt);
        Assert.Null(expired.RevokedAt);
    }
}
