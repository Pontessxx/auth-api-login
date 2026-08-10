using auth_api_login.Application.DTOs.Files;

namespace auth_api_login.Controllers.V1;

/// <summary>Upload de imagens.</summary>
[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/files")]
public class FilesController(IFileStorageService fileStorageService) : ControllerBase
{
    private readonly IFileStorageService _fileStorageService = fileStorageService;

    /// <summary>Envia uma imagem (PNG ou JPEG, até 5 MB) e retorna a URL onde ela ficará hospedada.</summary>
    /// <remarks>Rota autorizada — requer um Bearer token válido. Recebe o arquivo via multipart/form-data.</remarks>
    /// <response code="201">Imagem enviada com sucesso; retorna a URL de hospedagem.</response>
    /// <response code="400">Arquivo ausente, vazio, maior que o permitido ou de tipo não suportado.</response>
    /// <response code="401">Token ausente, inválido ou revogado.</response>
    [Authorize]
    [HttpPost("upload-image")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadImageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UploadImageResponse>> UploadImage(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);

        var request = new UploadImageRequest
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            Content = memoryStream.ToArray()
        };

        var result = await _fileStorageService.UploadImageAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        return result.ToProblem();
    }
}
