namespace auth_api_login.Tests.Infrastructure.Security;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ThenVerify_WithCorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("1234567");

        Assert.True(_hasher.Verify("1234567", hash));
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("1234567");

        Assert.False(_hasher.Verify("wrong-password", hash));
    }

    [Fact]
    public void Hash_ProducesDifferentOutputForSamePassword()
    {
        var hash1 = _hasher.Hash("1234567");
        var hash2 = _hasher.Hash("1234567");

        Assert.NotEqual(hash1, hash2);
    }

    [Theory]
    [InlineData("not-enough-parts")]
    [InlineData("one.two")]
    [InlineData("not-a-number.salt.hash")]
    public void Verify_WithMalformedHash_ReturnsFalse(string malformedHash)
    {
        Assert.False(_hasher.Verify("1234567", malformedHash));
    }
}
