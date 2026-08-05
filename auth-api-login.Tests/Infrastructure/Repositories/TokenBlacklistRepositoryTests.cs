namespace auth_api_login.Tests.Infrastructure.Repositories;

public class TokenBlacklistRepositoryTests
{
    [Fact]
    public async Task RevokeAsync_AddsNewEntry()
    {
        using var dbContext = TestDbContextFactory.Create();
        var repository = new TokenBlacklistRepository(dbContext);

        await repository.RevokeAsync("jti-1", DateTime.UtcNow.AddMinutes(15));

        Assert.Equal(1, dbContext.RevokedTokens.Count());
    }

    [Fact]
    public async Task RevokeAsync_WhenAlreadyRevoked_DoesNotDuplicate()
    {
        using var dbContext = TestDbContextFactory.Create();
        var repository = new TokenBlacklistRepository(dbContext);
        var expiresAt = DateTime.UtcNow.AddMinutes(15);

        await repository.RevokeAsync("jti-1", expiresAt);
        await repository.RevokeAsync("jti-1", expiresAt);

        Assert.Equal(1, dbContext.RevokedTokens.Count());
    }

    [Fact]
    public async Task IsRevokedAsync_WhenPresent_ReturnsTrue()
    {
        using var dbContext = TestDbContextFactory.Create();
        var repository = new TokenBlacklistRepository(dbContext);
        await repository.RevokeAsync("jti-1", DateTime.UtcNow.AddMinutes(15));

        var isRevoked = await repository.IsRevokedAsync("jti-1");

        Assert.True(isRevoked);
    }

    [Fact]
    public async Task IsRevokedAsync_WhenAbsent_ReturnsFalse()
    {
        using var dbContext = TestDbContextFactory.Create();
        var repository = new TokenBlacklistRepository(dbContext);

        var isRevoked = await repository.IsRevokedAsync("missing-jti");

        Assert.False(isRevoked);
    }
}
