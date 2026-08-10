using auth_api_login.Application.DTOs.Files;

namespace auth_api_login.Application.Interfaces;

public interface IFileStorageService
{
    Task<Result<UploadImageResponse>> UploadImageAsync(UploadImageRequest request, CancellationToken cancellationToken = default);
}
