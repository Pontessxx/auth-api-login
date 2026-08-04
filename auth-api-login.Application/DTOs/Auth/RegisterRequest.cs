using System.ComponentModel.DataAnnotations;

namespace auth_api_login.Application.DTOs.Auth;

public class RegisterRequest
{
    [Required]
    [StringLength(64, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(20, MinimumLength = 7)]
    [RegularExpression(@"^\d+$", ErrorMessage = "A senha deve conter apenas números, sem caracteres especiais.")]
    public string Password { get; set; } = string.Empty;
}
