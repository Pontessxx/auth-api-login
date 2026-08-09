using auth_api_login.Application.DTOs.Users;
using auth_api_login.Application.Interfaces;
using auth_api_login.Application.Mappings;
using auth_api_login.Domain.Exceptions;

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

    public async Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new UserNotFoundException(id);

        return user.ToResponse();
    }

    public async Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new UserNotFoundException(id);

        if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (existingUser is not null)
            {
                _logger.LogWarning(
                    "Atualização recusada para o usuário {UserId}: e-mail {Email} já está em uso.",
                    id,
                    request.Email);
                throw new EmailAlreadyExistsException(request.Email);
            }
        }

        user.Username = request.Username;
        user.Email = request.Email;

        await _userRepository.UpdateAsync(user, cancellationToken);
        _logger.LogInformation("Usuário {UserId} atualizado.", id);

        return user.ToResponse();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new UserNotFoundException(id);

        await _userRepository.DeleteAsync(user, cancellationToken);
        _logger.LogInformation("Usuário {UserId} excluído.", id);
    }
}
