namespace auth_api_login.Domain.Exceptions;

public class UserNotFoundException : DomainException
{
    public UserNotFoundException(Guid id)
        : base($"Usuário '{id}' não encontrado.")
    {
    }
}
