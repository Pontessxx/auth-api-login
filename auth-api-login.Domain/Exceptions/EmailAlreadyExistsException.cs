namespace auth_api_login.Domain.Exceptions;

public class EmailAlreadyExistsException : DomainException
{
    public EmailAlreadyExistsException(string email)
        : base($"Já existe um usuário cadastrado com o e-mail '{email}'.")
    {
    }
}
