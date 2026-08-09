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
        var response = await service.GetByIdAsync(user.Id, CancellationToken.None);

        Assert.Equal(user.Id, response.Id);
        Assert.Equal(user.Username, response.Username);
        Assert.Equal(user.Email, response.Email);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserMissing_ThrowsUserNotFoundException()
    {
        var id = Guid.NewGuid();
        _userRepository.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<UserNotFoundException>(() => service.GetByIdAsync(id, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_WhenUserMissing_ThrowsUserNotFoundException()
    {
        var id = Guid.NewGuid();
        var request = new UpdateUserRequest { Username = "new", Email = "new@test.com" };
        _userRepository.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<UserNotFoundException>(() => service.UpdateAsync(id, request, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_WhenEmailUnchanged_DoesNotCheckForConflictAndUpdates()
    {
        var user = new User { Id = Guid.NewGuid(), Username = "john", Email = "john@test.com" };
        var request = new UpdateUserRequest { Username = "john-updated", Email = "JOHN@test.com" };
        _userRepository.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var service = CreateService();
        var response = await service.UpdateAsync(user.Id, request, CancellationToken.None);

        Assert.Equal("john-updated", response.Username);
        Assert.Equal("JOHN@test.com", response.Email);
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
        var response = await service.UpdateAsync(user.Id, request, CancellationToken.None);

        Assert.Equal("new@test.com", response.Email);
        _userRepository.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenEmailChangedAndTaken_ThrowsEmailAlreadyExistsException()
    {
        var user = new User { Id = Guid.NewGuid(), Username = "john", Email = "john@test.com" };
        var request = new UpdateUserRequest { Username = "john", Email = "taken@test.com" };
        _userRepository.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _userRepository
            .Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = Guid.NewGuid(), Email = request.Email });

        var service = CreateService();

        await Assert.ThrowsAsync<EmailAlreadyExistsException>(() => service.UpdateAsync(user.Id, request, CancellationToken.None));
        _userRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserExists_DeletesUser()
    {
        var user = new User { Id = Guid.NewGuid() };
        _userRepository.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var service = CreateService();
        await service.DeleteAsync(user.Id, CancellationToken.None);

        _userRepository.Verify(x => x.DeleteAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserMissing_ThrowsUserNotFoundException()
    {
        var id = Guid.NewGuid();
        _userRepository.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<UserNotFoundException>(() => service.DeleteAsync(id, CancellationToken.None));
    }
}
