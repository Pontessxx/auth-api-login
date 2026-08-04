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

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        ITokenBlacklistRepository tokenBlacklistRepository,
        IRefreshTokenRepository refreshTokenRepository,
        JwtSettings jwtSettings)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _tokenBlacklistRepository = tokenBlacklistRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtSettings = jwtSettings;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser is not null)
        {
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

        return await BuildAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken)
            ?? throw new InvalidCredentialsException();

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        return await BuildAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = _refreshTokenGenerator.Hash(request.RefreshToken);
        var storedToken = await _refreshTokenRepository.GetByHashAsync(tokenHash, cancellationToken)
            ?? throw new InvalidCredentialsException();

        if (storedToken.RevokedAt is not null)
        {
            // Um refresh token já rotacionado/revogado sendo reapresentado é sinal de roubo:
            // encerra todas as sessões do usuário em vez de apenas negar essa tentativa.
            await _refreshTokenRepository.RevokeAllActiveAsync(storedToken.UserId, cancellationToken);
            throw new InvalidCredentialsException();
        }

        if (storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new InvalidCredentialsException();
        }

        var user = await _userRepository.GetByIdAsync(storedToken.UserId, cancellationToken)
            ?? throw new InvalidCredentialsException();

        var (response, newRefreshToken) = await BuildAuthResponseInternalAsync(user, cancellationToken);
        await _refreshTokenRepository.RevokeAsync(storedToken, newRefreshToken.Id, cancellationToken);

        return response;
    }

    public async Task LogoutAsync(Guid userId, string jti, DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        await _tokenBlacklistRepository.RevokeAsync(jti, expiresAt, cancellationToken);
        await _refreshTokenRepository.RevokeAllActiveAsync(userId, cancellationToken);
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
