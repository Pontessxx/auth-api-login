namespace auth_api_login.Application.DTOs.Auth;

public class ValidateResponse
{
    public Guid UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
}
