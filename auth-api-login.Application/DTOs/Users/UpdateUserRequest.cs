namespace auth_api_login.Application.DTOs.Users;

public class UpdateUserRequest
{
    [Required]
    [StringLength(64, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;
}
