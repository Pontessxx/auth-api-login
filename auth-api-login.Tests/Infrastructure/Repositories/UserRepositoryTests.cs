namespace auth_api_login.Tests.Infrastructure.Repositories;

public class UserRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsUser()
    {
        using var dbContext = TestDbContextFactory.Create();
        var repository = new UserRepository(dbContext);
        var user = new User { Id = Guid.NewGuid(), Username = "john", Email = "john@test.com", PasswordHash = "hash" };

        await repository.AddAsync(user);

        Assert.Equal(1, dbContext.Users.Count());
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsUser()
    {
        using var dbContext = TestDbContextFactory.Create();
        var user = new User { Id = Guid.NewGuid(), Username = "john", Email = "john@test.com", PasswordHash = "hash" };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var repository = new UserRepository(dbContext);
        var found = await repository.GetByIdAsync(user.Id);

        Assert.NotNull(found);
        Assert.Equal(user.Username, found!.Username);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        using var dbContext = TestDbContextFactory.Create();
        var repository = new UserRepository(dbContext);

        var found = await repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(found);
    }

    [Fact]
    public async Task GetByEmailAsync_WhenExists_ReturnsUser()
    {
        using var dbContext = TestDbContextFactory.Create();
        var user = new User { Id = Guid.NewGuid(), Username = "john", Email = "john@test.com", PasswordHash = "hash" };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var repository = new UserRepository(dbContext);
        var found = await repository.GetByEmailAsync("john@test.com");

        Assert.NotNull(found);
        Assert.Equal(user.Id, found!.Id);
    }

    [Fact]
    public async Task GetByEmailAsync_WhenMissing_ReturnsNull()
    {
        using var dbContext = TestDbContextFactory.Create();
        var repository = new UserRepository(dbContext);

        var found = await repository.GetByEmailAsync("missing@test.com");

        Assert.Null(found);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        using var dbContext = TestDbContextFactory.Create();
        var user = new User { Id = Guid.NewGuid(), Username = "john", Email = "john@test.com", PasswordHash = "hash" };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var repository = new UserRepository(dbContext);
        var tracked = await repository.GetByIdAsync(user.Id);
        tracked!.Username = "updated";
        await repository.UpdateAsync(tracked);

        dbContext.ChangeTracker.Clear();
        var reloaded = await repository.GetByIdAsync(user.Id);
        Assert.Equal("updated", reloaded!.Username);
    }

    [Fact]
    public async Task DeleteAsync_RemovesUser()
    {
        using var dbContext = TestDbContextFactory.Create();
        var user = new User { Id = Guid.NewGuid(), Username = "john", Email = "john@test.com", PasswordHash = "hash" };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var repository = new UserRepository(dbContext);
        await repository.DeleteAsync(user);

        Assert.Equal(0, dbContext.Users.Count());
    }
}
