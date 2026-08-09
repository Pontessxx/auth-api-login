namespace auth_api_login.Tests.Application.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();
    private readonly Mock<IRefreshTokenGenerator> _refreshTokenGenerator = new();
    private readonly Mock<ITokenBlacklistRepository> _tokenBlacklistRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly JwtSettings _jwtSettings = new()
    {
        MaxActiveSessionsPerUser = 5,
        RefreshTokenExpirationDays = 7
    };

    private AuthService CreateService() => new(
        _userRepository.Object,
        _passwordHasher.Object,
        _jwtTokenGenerator.Object,
        _refreshTokenGenerator.Object,
        _tokenBlacklistRepository.Object,
        _refreshTokenRepository.Object,
        _jwtSettings,
        NullLogger<AuthService>.Instance);

    private void SetupHappyTokenPath(int activeSessions = 0)
    {
        _jwtTokenGenerator
            .Setup(x => x.GenerateToken(It.IsAny<User>()))
            .Returns(("access-token", DateTime.UtcNow.AddMinutes(15)));

        _refreshTokenRepository
            .Setup(x => x.CountActiveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeSessions);

        _refreshTokenGenerator
            .Setup(x => x.Generate())
            .Returns(("raw-refresh-token", "hashed-refresh-token"));
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailNotTaken_CreatesUserAndReturnsAuthResponse()
    {
        var request = new RegisterRequest { Username = "john", Email = "john@test.com", Password = "1234567" };

        _userRepository
            .Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordHasher.Setup(x => x.Hash(request.Password)).Returns("hashed-password");
        SetupHappyTokenPath();

        var service = CreateService();

        var result = await service.RegisterAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token", result.Value.AccessToken);
        Assert.Equal("raw-refresh-token", result.Value.RefreshToken);
        Assert.Equal("john", result.Value.User.Username);
        Assert.Equal("john@test.com", result.Value.User.Email);

        _userRepository.Verify(x => x.AddAsync(
            It.Is<User>(u => u.Username == "john" && u.Email == "john@test.com" && u.PasswordHash == "hashed-password"),
            It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenRepository.Verify(x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ReturnsConflict()
    {
        var request = new RegisterRequest { Username = "john", Email = "john@test.com", Password = "1234567" };
        _userRepository
            .Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Email = request.Email });

        var service = CreateService();

        var result = await service.RegisterAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Conflict, result.Error!.Value.Type);
        _userRepository.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenUserNotFound_ReturnsUnauthorized()
    {
        var request = new LoginRequest { Email = "missing@test.com", Password = "1234567" };
        _userRepository
            .Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = CreateService();

        var result = await service.LoginAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Unauthorized, result.Error!.Value.Type);
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordInvalid_ReturnsUnauthorized()
    {
        var request = new LoginRequest { Email = "john@test.com", Password = "wrong" };
        var user = new User { Email = request.Email, PasswordHash = "hashed" };
        _userRepository
            .Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(x => x.Verify(request.Password, user.PasswordHash)).Returns(false);

        var service = CreateService();

        var result = await service.LoginAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Unauthorized, result.Error!.Value.Type);
    }

    [Fact]
    public async Task LoginAsync_WhenValid_ReturnsAuthResponse()
    {
        var request = new LoginRequest { Email = "john@test.com", Password = "1234567" };
        var user = new User { Id = Guid.NewGuid(), Username = "john", Email = request.Email, PasswordHash = "hashed" };
        _userRepository
            .Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(x => x.Verify(request.Password, user.PasswordHash)).Returns(true);
        SetupHappyTokenPath();

        var service = CreateService();

        var result = await service.LoginAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token", result.Value.AccessToken);
        Assert.Equal(user.Id, result.Value.User.Id);
        _userRepository.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenActiveSessionsAtLimit_RevokesOldestSession()
    {
        _jwtSettings.MaxActiveSessionsPerUser = 2;
        var request = new LoginRequest { Email = "john@test.com", Password = "1234567" };
        var user = new User { Id = Guid.NewGuid(), Username = "john", Email = request.Email, PasswordHash = "hashed" };
        var oldest = new RefreshToken { Id = Guid.NewGuid(), UserId = user.Id };

        _userRepository.Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(x => x.Verify(request.Password, user.PasswordHash)).Returns(true);
        SetupHappyTokenPath(activeSessions: 2);
        _refreshTokenRepository
            .Setup(x => x.GetOldestActiveAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(oldest);

        var service = CreateService();
        await service.LoginAsync(request, CancellationToken.None);

        _refreshTokenRepository.Verify(x => x.RevokeAsync(oldest, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WhenActiveSessionsAtLimitButNoOldestFound_DoesNotRevoke()
    {
        _jwtSettings.MaxActiveSessionsPerUser = 2;
        var request = new LoginRequest { Email = "john@test.com", Password = "1234567" };
        var user = new User { Id = Guid.NewGuid(), Username = "john", Email = request.Email, PasswordHash = "hashed" };

        _userRepository.Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(x => x.Verify(request.Password, user.PasswordHash)).Returns(true);
        SetupHappyTokenPath(activeSessions: 2);
        _refreshTokenRepository
            .Setup(x => x.GetOldestActiveAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var service = CreateService();
        await service.LoginAsync(request, CancellationToken.None);

        _refreshTokenRepository.Verify(x => x.RevokeAsync(It.IsAny<RefreshToken>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_WhenTokenNotFound_ReturnsUnauthorized()
    {
        var request = new RefreshRequest { RefreshToken = "raw" };
        _refreshTokenGenerator.Setup(x => x.Hash(request.RefreshToken)).Returns("hash");
        _refreshTokenRepository
            .Setup(x => x.GetByHashAsync("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var service = CreateService();

        var result = await service.RefreshAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Unauthorized, result.Error!.Value.Type);
    }

    [Fact]
    public async Task RefreshAsync_WhenTokenAlreadyRevoked_RevokesAllActiveSessionsAndReturnsUnauthorized()
    {
        var request = new RefreshRequest { RefreshToken = "raw" };
        var userId = Guid.NewGuid();
        var storedToken = new RefreshToken { UserId = userId, RevokedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(1) };

        _refreshTokenGenerator.Setup(x => x.Hash(request.RefreshToken)).Returns("hash");
        _refreshTokenRepository
            .Setup(x => x.GetByHashAsync("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        var service = CreateService();

        var result = await service.RefreshAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Unauthorized, result.Error!.Value.Type);

        _refreshTokenRepository.Verify(x => x.RevokeAllActiveAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _userRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_WhenTokenExpired_ReturnsUnauthorized()
    {
        var request = new RefreshRequest { RefreshToken = "raw" };
        var storedToken = new RefreshToken { UserId = Guid.NewGuid(), RevokedAt = null, ExpiresAt = DateTime.UtcNow.AddMinutes(-1) };

        _refreshTokenGenerator.Setup(x => x.Hash(request.RefreshToken)).Returns("hash");
        _refreshTokenRepository
            .Setup(x => x.GetByHashAsync("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        var service = CreateService();

        var result = await service.RefreshAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Unauthorized, result.Error!.Value.Type);
        _refreshTokenRepository.Verify(x => x.RevokeAllActiveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_WhenUserNotFound_ReturnsUnauthorized()
    {
        var request = new RefreshRequest { RefreshToken = "raw" };
        var storedToken = new RefreshToken { UserId = Guid.NewGuid(), RevokedAt = null, ExpiresAt = DateTime.UtcNow.AddDays(1) };

        _refreshTokenGenerator.Setup(x => x.Hash(request.RefreshToken)).Returns("hash");
        _refreshTokenRepository
            .Setup(x => x.GetByHashAsync("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);
        _userRepository
            .Setup(x => x.GetByIdAsync(storedToken.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = CreateService();

        var result = await service.RefreshAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Unauthorized, result.Error!.Value.Type);
    }

    [Fact]
    public async Task RefreshAsync_WhenValid_RotatesTokenAndReturnsResponse()
    {
        var request = new RefreshRequest { RefreshToken = "raw" };
        var user = new User { Id = Guid.NewGuid(), Username = "john", Email = "john@test.com" };
        var storedToken = new RefreshToken { UserId = user.Id, RevokedAt = null, ExpiresAt = DateTime.UtcNow.AddDays(1) };

        _refreshTokenGenerator.Setup(x => x.Hash(request.RefreshToken)).Returns("hash");
        _refreshTokenRepository
            .Setup(x => x.GetByHashAsync("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);
        _userRepository.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        SetupHappyTokenPath();

        RefreshToken? capturedNewToken = null;
        _refreshTokenRepository
            .Setup(x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((token, _) => capturedNewToken = token)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var result = await service.RefreshAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token", result.Value.AccessToken);
        Assert.Equal("raw-refresh-token", result.Value.RefreshToken);
        Assert.NotNull(capturedNewToken);
        _refreshTokenRepository.Verify(x => x.RevokeAsync(storedToken, capturedNewToken!.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_RevokesBlacklistAndAllActiveSessions()
    {
        var userId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddMinutes(15);

        var service = CreateService();
        await service.LogoutAsync(userId, "jti-value", expiresAt, CancellationToken.None);

        _tokenBlacklistRepository.Verify(x => x.RevokeAsync("jti-value", expiresAt, It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenRepository.Verify(x => x.RevokeAllActiveAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
