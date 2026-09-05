using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.DTOs.Auth;
using Application.Exceptions;
using MediatR;

namespace Application.Features.Auth.Commands.JoinStore;

public sealed class JoinStoreCommandHandler : IRequestHandler<JoinStoreCommand, JoinStoreResponse>
{
    private readonly IStoreUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;

    public JoinStoreCommandHandler(
        IStoreUserRepository userRepository,
        ICurrentUser currentUser)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task<JoinStoreResponse> Handle(
        JoinStoreCommand command,
        CancellationToken cancellationToken)
    {
        if (await _userRepository.EmailExistsAsync(command.Email, cancellationToken))
        {
            throw new ConflictException("A user with this email already exists.");
        }

        Guid staffId;
        try
        {
            staffId = await _userRepository.CreateStaffAsync(
                _currentUser.StoreId,
                _currentUser.VerticalCategory,
                command.Email,
                command.Password,
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new BadRequestException(ex.Message);
        }

        return new JoinStoreResponse(staffId, command.Email);
    }
}