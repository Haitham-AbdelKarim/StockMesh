using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Auth;
using MediatR;

namespace Application.Features.Auth.Commands.JoinStore;

public sealed class JoinStoreCommandHandler : IRequestHandler<JoinStoreCommand, Result<JoinStoreResponse>>
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

    public async Task<Result<JoinStoreResponse>> Handle(
        JoinStoreCommand command,
        CancellationToken cancellationToken)
    {
        if (await _userRepository.EmailExistsAsync(command.Email, cancellationToken))
        {
            return Result<JoinStoreResponse>.Conflict("A user with this email already exists.");
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
            return Result<JoinStoreResponse>.BadRequest(ex.Message);
        }

        return Result<JoinStoreResponse>.Success(new JoinStoreResponse(staffId, command.Email));
    }
}