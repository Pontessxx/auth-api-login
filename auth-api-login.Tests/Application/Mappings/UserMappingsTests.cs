namespace auth_api_login.Tests.Application.Mappings;

public class UserMappingsTests
{
    [Fact]
    public void ToResponse_MapsAllFields()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "john",
            Email = "john@test.com",
            PasswordHash = "should-not-be-mapped",
            CreatedAt = DateTime.UtcNow
        };

        var response = user.ToResponse();

        Assert.Equal(user.Id, response.Id);
        Assert.Equal(user.Username, response.Username);
        Assert.Equal(user.Email, response.Email);
        Assert.Equal(user.CreatedAt, response.CreatedAt);
    }
}
