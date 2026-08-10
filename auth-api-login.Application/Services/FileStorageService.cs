namespace auth_api_login.Application.Services;

public class FileStorageService : IFileStorageService
{
    private readonly FileStorageOptions _options;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(FileStorageOptions options, ILogger<FileStorageService> logger)
    {
        _options = options;
        _logger = logger;
    }

    public Task<Result<UploadImageResponse>> UploadImageAsync(UploadImageRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Content.Length == 0)
        {
            _logger.LogWarning("Upload recusado: arquivo vazio.");
            return Task.FromResult(Result<UploadImageResponse>.Validation("O arquivo enviado está vazio."));
        }

        if (request.Content.Length > _options.MaxFileSizeInBytes)
        {
            _logger.LogWarning(
                "Upload recusado: arquivo com {SizeInBytes} bytes excede o limite de {MaxFileSizeInBytes} bytes.",
                request.Content.Length,
                _options.MaxFileSizeInBytes);
            return Task.FromResult(Result<UploadImageResponse>.Validation(
                $"O arquivo excede o tamanho máximo permitido de {_options.MaxFileSizeInBytes} bytes."));
        }

        if (!_options.AllowedContentTypes.Contains(request.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Upload recusado: content-type {ContentType} não permitido.", request.ContentType);
            return Task.FromResult(Result<UploadImageResponse>.Validation(
                $"Tipo de arquivo '{request.ContentType}' não permitido. Tipos aceitos: {string.Join(", ", _options.AllowedContentTypes)}."));
        }

        var extension = Path.GetExtension(request.FileName);
        var storedFileName = $"{Guid.CreateVersion7()}{extension}";
        var url = $"{_options.BaseUrl.TrimEnd('/')}/{storedFileName}";

        _logger.LogInformation(
            "Imagem {FileName} ({SizeInBytes} bytes) armazenada como {StoredFileName}.",
            request.FileName,
            request.Content.Length,
            storedFileName);

        return Task.FromResult(Result<UploadImageResponse>.Success(new UploadImageResponse
        {
            Url = url,
            FileName = storedFileName,
            SizeInBytes = request.Content.Length,
            CreatedAt = DateTime.UtcNow
        }));
    }
}
