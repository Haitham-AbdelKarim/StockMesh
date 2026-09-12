using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Payments;
using MediatR;

namespace Application.Features.Payments.Queries.GetConnectStatus;

public sealed class GetConnectStatusQueryHandler :
    IRequestHandler<GetConnectStatusQuery, Result<ConnectStatusResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IStoreRepository _storeRepository;
    private readonly IPaymentService _paymentService;

    public GetConnectStatusQueryHandler(
        ICurrentUser currentUser,
        IStoreRepository storeRepository,
        IPaymentService paymentService)
    {
        _currentUser = currentUser;
        _storeRepository = storeRepository;
        _paymentService = paymentService;
    }

    public async Task<Result<ConnectStatusResponse>> Handle(
        GetConnectStatusQuery query,
        CancellationToken cancellationToken)
    {
        var store = await _storeRepository.GetByIdAsync(_currentUser.StoreId, cancellationToken);

        if (store is null)
        {
            return Result<ConnectStatusResponse>.NotFound("Store not found.");
        }

        if (string.IsNullOrWhiteSpace(store.StripeConnectAccountId))
        {
            return Result<ConnectStatusResponse>.Success(new ConnectStatusResponse(null, false));
        }

        var status = await _paymentService.GetAccountStatusAsync(
            store.StripeConnectAccountId,
            cancellationToken);

        store.SetPayoutStatus(status.PayoutsEnabled);
        await _storeRepository.SaveChangesAsync(cancellationToken);

        return Result<ConnectStatusResponse>.Success(status);
    }
}