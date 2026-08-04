using System.ComponentModel.DataAnnotations;

namespace auth_api_login.Application.DTOs.Auth;

public class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
