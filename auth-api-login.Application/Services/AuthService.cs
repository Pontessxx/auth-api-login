using auth_api_login.Application.Common;
using auth_api_login.Application.DTOs.Auth;
using auth_api_login.Application.Interfaces;
using auth_api_login.Application.Mappings;
using auth_api_login.Domain.Exceptions;

namespace auth_api_login.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly ITokenBlacklistRepository _tokenBlacklistRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        ITokenBlacklistRepository tokenBlacklistRepository,
        IRefreshTokenRepository refreshTokenRepository,
        JwtSettings jwtSettings,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _tokenBlacklistRepository = tokenBlacklistRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtSettings = jwtSettings;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser is not null)
        {
            _logger.LogWarning("Registro recusado: já existe um usuário com o e-mail {Email}.", request.Email);
            throw new EmailAlreadyExistsException(request.Email);
        }

        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user, cancellationToken);
        _logger.LogInformation("Usuário {UserId} registrado com sucesso ({Email}).", user.Id, user.Email);

        return await BuildAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("Login falhou: nenhum usuário encontrado para o e-mail {Email}.", request.Email);
            throw new InvalidCredentialsException();
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login falhou: senha inválida para o usuário {UserId}.", user.Id);
            throw new InvalidCredentialsException();
        }

        _logger.LogInformation("Usuário {UserId} autenticado com sucesso.", user.Id);

        return await BuildAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = _refreshTokenGenerator.Hash(request.RefreshToken);
        var storedToken = await _refreshTokenRepository.GetByHashAsync(tokenHash, cancellationToken);
        if (storedToken is null)
        {
            _logger.LogWarning("Refresh falhou: token não reconhecido.");
            throw new InvalidCredentialsException();
        }

        if (storedToken.RevokedAt is not null)
        {
            // Um refresh token já rotacionado/revogado sendo reapresentado é sinal de roubo:
            // encerra todas as sessões do usuário em vez de apenas negar essa tentativa.
            _logger.LogWarning(
                "Reuso de refresh token detectado para o usuário {UserId}; todas as sessões foram revogadas.",
                storedToken.UserId);
            await _refreshTokenRepository.RevokeAllActiveAsync(storedToken.UserId, cancellationToken);
            throw new InvalidCredentialsException();
        }

        if (storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            _logger.LogWarning("Refresh falhou: token expirado para o usuário {UserId}.", storedToken.UserId);
            throw new InvalidCredentialsException();
        }

        var user = await _userRepository.GetByIdAsync(storedToken.UserId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("Refresh falhou: usuário {UserId} não encontrado.", storedToken.UserId);
            throw new InvalidCredentialsException();
        }

        var (response, newRefreshToken) = await BuildAuthResponseInternalAsync(user, cancellationToken);
        await _refreshTokenRepository.RevokeAsync(storedToken, newRefreshToken.Id, cancellationToken);
        _logger.LogInformation("Tokens renovados com sucesso para o usuário {UserId}.", user.Id);

        return response;
    }

    public async Task LogoutAsync(Guid userId, string jti, DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        await _tokenBlacklistRepository.RevokeAsync(jti, expiresAt, cancellationToken);
        await _refreshTokenRepository.RevokeAllActiveAsync(userId, cancellationToken);
        _logger.LogInformation("Logout realizado: todas as sessões do usuário {UserId} foram revogadas.", userId);
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(User user, CancellationToken cancellationToken)
    {
        var (response, _) = await BuildAuthResponseInternalAsync(user, cancellationToken);
        return response;
    }

    private async Task<(AuthResponse Response, RefreshToken RefreshToken)> BuildAuthResponseInternalAsync(User user, CancellationToken cancellationToken)
    {
        var (accessToken, accessExpiresAt) = _jwtTokenGenerator.GenerateToken(user);

        var activeSessions = await _refreshTokenRepository.CountActiveAsync(user.Id, cancellationToken);
        if (activeSessions >= _jwtSettings.MaxActiveSessionsPerUser)
        {
            var oldestSession = await _refreshTokenRepository.GetOldestActiveAsync(user.Id, cancellationToken);
            if (oldestSession is not null)
            {
                await _refreshTokenRepository.RevokeAsync(oldestSession, cancellationToken: cancellationToken);
                _logger.LogInformation(
                    "Limite de sessões ativas atingido para o usuário {UserId}; sessão mais antiga revogada.",
                    user.Id);
            }
        }

        var (rawRefreshToken, refreshTokenHash) = _refreshTokenGenerator.Generate();
        var refreshExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = refreshExpiresAt
        };

        await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);

        var response = new AuthResponse
        {
            AccessToken = accessToken,
            ExpiresAt = accessExpiresAt,
            RefreshToken = rawRefreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt,
            User = user.ToResponse()
        };

        return (response, refreshTokenEntity);
    }
}
