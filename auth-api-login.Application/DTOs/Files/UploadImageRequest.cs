namespace auth_api_login.Application.DTOs.Files;

public class UploadImageRequest
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = [];
}
