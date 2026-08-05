namespace auth_api_login.Tests.Domain.Entities;

public class UserTests
{
    [Fact]
    public void DefaultValues_AreEmptyStrings()
    {
        var user = new User();

        Assert.Equal(string.Empty, user.Username);
        Assert.Equal(string.Empty, user.Email);
        Assert.Equal(string.Empty, user.PasswordHash);
        Assert.Equal(Guid.Empty, user.Id);
        Assert.Equal(default, user.CreatedAt);
    }

    [Fact]
    public void Properties_CanBeSetAndRead()
    {
        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        var user = new User
        {
            Id = id,
            Username = "john",
            Email = "john@test.com",
            PasswordHash = "hash",
            CreatedAt = createdAt
        };

        Assert.Equal(id, user.Id);
        Assert.Equal("john", user.Username);
        Assert.Equal("john@test.com", user.Email);
        Assert.Equal("hash", user.PasswordHash);
        Assert.Equal(createdAt, user.CreatedAt);
    }
}

public class RefreshTokenTests
{
    [Fact]
    public void DefaultValues_AreExpected()
    {
        var token = new RefreshToken();

        Assert.Equal(string.Empty, token.TokenHash);
        Assert.Null(token.RevokedAt);
        Assert.Null(token.ReplacedByTokenId);
    }

    [Fact]
    public void Properties_CanBeSetAndRead()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var replacedBy = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var expiresAt = createdAt.AddDays(7);
        var revokedAt = createdAt.AddDays(1);

        var token = new RefreshToken
        {
            Id = id,
            UserId = userId,
            TokenHash = "hash",
            CreatedAt = createdAt,
            ExpiresAt = expiresAt,
            RevokedAt = revokedAt,
            ReplacedByTokenId = replacedBy
        };

        Assert.Equal(id, token.Id);
        Assert.Equal(userId, token.UserId);
        Assert.Equal("hash", token.TokenHash);
        Assert.Equal(createdAt, token.CreatedAt);
        Assert.Equal(expiresAt, token.ExpiresAt);
        Assert.Equal(revokedAt, token.RevokedAt);
        Assert.Equal(replacedBy, token.ReplacedByTokenId);
    }
}

public class RevokedTokenTests
{
    [Fact]
    public void DefaultValues_AreExpected()
    {
        var token = new RevokedToken();

        Assert.Equal(string.Empty, token.Jti);
    }

    [Fact]
    public void Properties_CanBeSetAndRead()
    {
        var id = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddMinutes(15);
        var revokedAt = DateTime.UtcNow;

        var token = new RevokedToken
        {
            Id = id,
            Jti = "jti-value",
            ExpiresAt = expiresAt,
            RevokedAt = revokedAt
        };

        Assert.Equal(id, token.Id);
        Assert.Equal("jti-value", token.Jti);
        Assert.Equal(expiresAt, token.ExpiresAt);
        Assert.Equal(revokedAt, token.RevokedAt);
    }
}
