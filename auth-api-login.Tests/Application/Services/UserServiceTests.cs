namespace auth_api_login.Tests.Application.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();

    private UserService CreateService() => new(_userRepository.Object, NullLogger<UserService>.Instance);

    [Fact]
    public async Task GetByIdAsync_WhenUserExists_ReturnsUserResponse()
    {
        var user = new User { Id = Guid.NewGuid(), Username = "john", Email = "john@test.com", CreatedAt = DateTime.UtcNow };
        _userRepository.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var service = CreateService();
        var result = await service.GetByIdAsync(user.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value.Id);
        Assert.Equal(user.Username, result.Value.Username);
        Assert.Equal(user.Email, result.Value.Email);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserMissing_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _userRepository.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var service = CreateService();

        var result = await service.GetByIdAsync(id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.NotFound, result.Error!.Value.Type);
    }

    [Fact]
    public async Task UpdateAsync_WhenUserMissing_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        var request = new UpdateUserRequest { Username = "new", Email = "new@test.com" };
        _userRepository.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var service = CreateService();

        var result = await service.UpdateAsync(id, request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.NotFound, result.Error!.Value.Type);
    }

    [Fact]
    public async Task UpdateAsync_WhenEmailUnchanged_DoesNotCheckForConflictAndUpdates()
    {
        var user = new User { Id = Guid.NewGuid(), Username = "john", Email = "john@test.com" };
        var request = new UpdateUserRequest { Username = "john-updated", Email = "JOHN@test.com" };
        _userRepository.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var service = CreateService();
        var result = await service.UpdateAsync(user.Id, request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("john-updated", result.Value.Username);
        Assert.Equal("JOHN@test.com", result.Value.Email);
        _userRepository.Verify(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _userRepository.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenEmailChangedAndNotTaken_Updates()
    {
        var user = new User { Id = Guid.NewGuid(), Username = "john", Email = "john@test.com" };
        var request = new UpdateUserRequest { Username = "john", Email = "new@test.com" };
        _userRepository.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _userRepository.Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var service = CreateService();
        var result = await service.UpdateAsync(user.Id, request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("new@test.com", result.Value.Email);
        _userRepository.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenEmailChangedAndTaken_ReturnsConflict()
    {
        var user = new User { Id = Guid.NewGuid(), Username = "john", Email = "john@test.com" };
        var request = new UpdateUserRequest { Username = "john", Email = "taken@test.com" };
        _userRepository.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _userRepository
            .Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = Guid.NewGuid(), Email = request.Email });

        var service = CreateService();

        var result = await service.UpdateAsync(user.Id, request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Conflict, result.Error!.Value.Type);
        _userRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserExists_DeletesUser()
    {
        var user = new User { Id = Guid.NewGuid() };
        _userRepository.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var service = CreateService();
        var result = await service.DeleteAsync(user.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _userRepository.Verify(x => x.DeleteAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserMissing_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _userRepository.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var service = CreateService();

        var result = await service.DeleteAsync(id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.NotFound, result.Error!.Value.Type);
    }
}
