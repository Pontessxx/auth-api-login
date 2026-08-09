namespace auth_api_login.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<UserResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return Result<UserResponse>.NotFound($"Usuário '{id}' não encontrado.");
        }

        return Result<UserResponse>.Success(user.ToResponse());
    }

    public async Task<Result<UserResponse>> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return Result<UserResponse>.NotFound($"Usuário '{id}' não encontrado.");
        }

        if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (existingUser is not null)
            {
                _logger.LogWarning(
                    "Atualização recusada para o usuário {UserId}: e-mail {Email} já está em uso.",
                    id,
                    request.Email);
                return Result<UserResponse>.Conflict($"Já existe um usuário cadastrado com o e-mail '{request.Email}'.");
            }
        }

        user.Username = request.Username;
        user.Email = request.Email;

        await _userRepository.UpdateAsync(user, cancellationToken);
        _logger.LogInformation("Usuário {UserId} atualizado.", id);

        return Result<UserResponse>.Success(user.ToResponse());
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return Result.NotFound($"Usuário '{id}' não encontrado.");
        }

        await _userRepository.DeleteAsync(user, cancellationToken);
        _logger.LogInformation("Usuário {UserId} excluído.", id);

        return Result.Success();
    }
}
