namespace auth_api_login.Domain.Exceptions;

public class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException()
        : base("E-mail ou senha inválidos.")
    {
    }
}
