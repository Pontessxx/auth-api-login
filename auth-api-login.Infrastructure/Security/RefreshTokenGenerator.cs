using System.Security.Cryptography;
using auth_api_login.Application.Interfaces;

namespace auth_api_login.Infrastructure.Security;

public class RefreshTokenGenerator : IRefreshTokenGenerator
{
    private const int TokenSizeInBytes = 64;

    public (string RawToken, string TokenHash) Generate()
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenSizeInBytes));
        return (rawToken, Hash(rawToken));
    }

    public string Hash(string rawToken)
    {
        var hashBytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hashBytes);
    }
}
