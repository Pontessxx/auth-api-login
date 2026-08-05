namespace auth_api_login.Tests.Domain.Exceptions;

public class DomainExceptionTests
{
    private sealed class TestDomainException(string message) : DomainException(message)
    {
    }

    [Fact]
    public void Constructor_SetsMessage()
    {
        var exception = new TestDomainException("boom");

        Assert.Equal("boom", exception.Message);
        Assert.IsAssignableFrom<Exception>(exception);
    }

    [Fact]
    public void EmailAlreadyExistsException_FormatsMessageWithEmail()
    {
        var exception = new EmailAlreadyExistsException("user@test.com");

        Assert.Equal("Já existe um usuário cadastrado com o e-mail 'user@test.com'.", exception.Message);
        Assert.IsAssignableFrom<DomainException>(exception);
    }

    [Fact]
    public void InvalidCredentialsException_HasFixedMessage()
    {
        var exception = new InvalidCredentialsException();

        Assert.Equal("E-mail ou senha inválidos.", exception.Message);
        Assert.IsAssignableFrom<DomainException>(exception);
    }

    [Fact]
    public void UserNotFoundException_FormatsMessageWithId()
    {
        var id = Guid.NewGuid();
        var exception = new UserNotFoundException(id);

        Assert.Equal($"Usuário '{id}' não encontrado.", exception.Message);
        Assert.IsAssignableFrom<DomainException>(exception);
    }
}
