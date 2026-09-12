using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Payments;
using MediatR;

namespace Application.Features.Payments.Commands.CreateConnectOnboarding;

public sealed class CreateConnectOnboardingCommandHandler :
    IRequestHandler<CreateConnectOnboardingCommand, Result<ConnectOnboardingResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IStoreRepository _storeRepository;
    private readonly IPaymentService _paymentService;

    public CreateConnectOnboardingCommandHandler(
        ICurrentUser currentUser,
        IStoreRepository storeRepository,
        IPaymentService paymentService)
    {
        _currentUser = currentUser;
        _storeRepository = storeRepository;
        _paymentService = paymentService;
    }

    public async Task<Result<ConnectOnboardingResponse>> Handle(
        CreateConnectOnboardingCommand command,
        CancellationToken cancellationToken)
    {
        var store = await _storeRepository.GetByIdAsync(_currentUser.StoreId, cancellationToken);

        if (store is null)
        {
            return Result<ConnectOnboardingResponse>.NotFound("Store not found.");
        }

        var link = await _paymentService.CreateOnboardingLinkAsync(
            store.StripeConnectAccountId,
            _currentUser.Email,
            cancellationToken);

        store.ConnectStripeAccount(link.AccountId);
        await _storeRepository.SaveChangesAsync(cancellationToken);

        return Result<ConnectOnboardingResponse>.Success(link);
    }
}