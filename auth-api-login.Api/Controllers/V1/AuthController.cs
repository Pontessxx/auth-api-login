using System.IdentityModel.Tokens.Jwt;
using auth_api_login.Application.DTOs.Auth;

namespace auth_api_login.Controllers.V1;

/// <summary>Cadastro, autenticação e revogação de sessão.</summary>
[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/auth-service")]
public class AuthController(IAuthService authService, ILogger<AuthController> logger) : ControllerBase
{
    private readonly IAuthService _authService = authService;
    private readonly ILogger<AuthController> _logger = logger;

    /// <summary>Cria um novo usuário e retorna o token de acesso.</summary>
    /// <remarks>Rota anônima — não requer autenticação.</remarks>
    /// <response code="201">Usuário criado; retorna o token de acesso e os dados do usuário.</response>
    /// <response code="400">Dados inválidos (ex.: senha fora do formato numérico exigido).</response>
    /// <response code="409">Já existe um usuário cadastrado com o e-mail informado.</response>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        return result.ToProblem();
    }

    /// <summary>Autentica um usuário e retorna o token de acesso.</summary>
    /// <remarks>Rota anônima — não requer autenticação.</remarks>
    /// <response code="200">Autenticado com sucesso; retorna o token de acesso e os dados do usuário.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">E-mail ou senha incorretos.</response>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return result.ToProblem();
    }

    /// <summary>Troca um refresh token válido por um novo par de tokens.</summary>
    /// <remarks>Rota anônima — não requer autenticação. O refresh token usado é revogado (rotação); reapresentá-lo depois disso revoga todas as sessões do usuário.</remarks>
    /// <response code="200">Novo par de tokens gerado com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">Refresh token ausente, inválido, expirado ou já revogado.</response>
    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return result.ToProblem();
    }

    /// <summary>Encerra a sessão do usuário autenticado por completo.</summary>
    /// <remarks>Rota autorizada — requer um Bearer token válido. Revoga o access token atual e todos os refresh tokens ativos do usuário; nenhuma sessão anterior permanece válida.</remarks>
    /// <response code="204">Sessões revogadas com sucesso.</response>
    /// <response code="401">Token ausente, inválido ou já revogado.</response>
    [Authorize]
    [HttpDelete("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var jti = User.FindFirstValue("jti");
        var expClaim = User.FindFirstValue("exp");
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrEmpty(jti) || !long.TryParse(expClaim, out var expUnixSeconds) || !Guid.TryParse(sub, out var userId))
        {
            _logger.LogWarning("Logout rejeitado: claims de usuário ausentes ou inválidas no token.");
            return ResultExtensions.UnauthorizedProblem("Usuário precisa estar autenticado.");
        }

        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expUnixSeconds).UtcDateTime;
        await _authService.LogoutAsync(userId, jti, expiresAt, cancellationToken);
        return NoContent();
    }

    /// <summary>Valida se o usuário está de fato autenticado.</summary>
    /// <remarks>Rota autorizada — requer um Bearer token válido. Se o token estiver ausente, inválido, expirado ou revogado, o pipeline de autenticação já responde 401 antes de chegar aqui.</remarks>
    /// <response code="200">Usuário autenticado; retorna o id do usuário e a expiração do token.</response>
    /// <response code="401">Token ausente, inválido, expirado ou revogado.</response>
    [Authorize]
    [HttpGet("validate")]
    [ProducesResponseType(typeof(ValidateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public IActionResult Validate()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var expClaim = User.FindFirstValue("exp");
        if (!Guid.TryParse(sub, out var userId) || !long.TryParse(expClaim, out var expUnixSeconds))
        {
            _logger.LogWarning("Validate rejeitado: claims de usuário ausentes ou inválidas no token.");
            return ResultExtensions.UnauthorizedProblem("Usuário precisa estar autenticado.");
        }

        var response = new ValidateResponse
        {
            UserId = userId,
            ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(expUnixSeconds).UtcDateTime
        };
        return Ok(response);
    }
}
