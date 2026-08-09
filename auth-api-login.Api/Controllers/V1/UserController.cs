using auth_api_login.Application.DTOs.Users;

namespace auth_api_login.Controllers.V1;

/// <summary>Consulta, atualização e exclusão dos dados do usuário autenticado.</summary>
[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/user")]
public class UserController(IUserService userService, ILogger<UserController> logger) : ControllerBase
{
    private readonly IUserService _userService = userService;
    private readonly ILogger<UserController> _logger = logger;

    /// <summary>Retorna os dados do usuário autenticado.</summary>
    /// <remarks>Rota autorizada — requer um Bearer token válido.</remarks>
    /// <response code="200">Dados do usuário autenticado.</response>
    /// <response code="401">Token ausente, inválido ou revogado.</response>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> GetMe(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            _logger.LogWarning("Requisição rejeitada: claim de usuário ausente ou inválida no token.");
            return ResultExtensions.UnauthorizedProblem("Usuário precisa estar autenticado.");
        }

        var result = await _userService.GetByIdAsync(userId, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return result.ToProblem();
    }

    /// <summary>Atualiza o username e o e-mail do usuário autenticado.</summary>
    /// <remarks>Rota autorizada — requer um Bearer token válido.</remarks>
    /// <response code="200">Usuário atualizado; retorna os dados atualizados.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">Token ausente, inválido ou revogado.</response>
    /// <response code="409">O e-mail informado já está em uso por outro usuário.</response>
    [Authorize]
    [HttpPut("edit")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> UpdateMe(
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            _logger.LogWarning("Requisição rejeitada: claim de usuário ausente ou inválida no token.");
            return ResultExtensions.UnauthorizedProblem("Usuário precisa estar autenticado.");
        }

        var result = await _userService.UpdateAsync(userId, request, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return result.ToProblem();
    }

    /// <summary>Exclui a conta do usuário autenticado.</summary>
    /// <remarks>Rota autorizada — requer um Bearer token válido. A exclusão é permanente e imediata.</remarks>
    /// <response code="204">Conta excluída com sucesso.</response>
    /// <response code="401">Token ausente, inválido ou revogado.</response>
    [Authorize]
    [HttpDelete("remove")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteMe(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            _logger.LogWarning("Requisição rejeitada: claim de usuário ausente ou inválida no token.");
            return ResultExtensions.UnauthorizedProblem("Usuário precisa estar autenticado.");
        }

        var result = await _userService.DeleteAsync(userId, cancellationToken);
        if (result.IsSuccess)
        {
            return NoContent();
        }

        return result.ToProblem();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var subject = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(subject, out userId);
    }
}
