namespace auth_api_login.Domain.Entities;

public class RevokedToken
{
    public Guid Id { get; set; }
    public string Jti { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime RevokedAt { get; set; }
}
