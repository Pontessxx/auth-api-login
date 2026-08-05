namespace auth_api_login.Tests.Infrastructure.Security;

public class RefreshTokenGeneratorTests
{
    private readonly RefreshTokenGenerator _generator = new();

    [Fact]
    public void Generate_ReturnsRawTokenAndMatchingHash()
    {
        var (rawToken, tokenHash) = _generator.Generate();

        Assert.NotEmpty(rawToken);
        Assert.NotEmpty(tokenHash);
        Assert.Equal(_generator.Hash(rawToken), tokenHash);
    }

    [Fact]
    public void Generate_ProducesUniqueValuesEachCall()
    {
        var (raw1, hash1) = _generator.Generate();
        var (raw2, hash2) = _generator.Generate();

        Assert.NotEqual(raw1, raw2);
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Hash_IsDeterministic()
    {
        var hash1 = _generator.Hash("same-input");
        var hash2 = _generator.Hash("same-input");

        Assert.Equal(hash1, hash2);
    }
}
