namespace auth_api_login.Application.Common;

public class FileStorageOptions
{
    public const string SectionName = "FileStorageOptions";

    public string BaseUrl { get; set; } = string.Empty;
    public long MaxFileSizeInBytes { get; set; } = 5 * 1024 * 1024;
    public List<string> AllowedContentTypes { get; set; } = [];
}
