namespace auth_api_login.Application.DTOs.Files;

public class UploadImageResponse
{
    public string Url { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long SizeInBytes { get; set; }
    public DateTime CreatedAt { get; set; }
}
