namespace auth_api_login.Application.Interfaces;

public interface IRefreshTokenGenerator
{
    (string RawToken, string TokenHash) Generate();
    string Hash(string rawToken);
}
