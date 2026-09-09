using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class DailyMarketSignal : BaseEntity
{
    public Guid ProductId { get; private set; }

    public VerticalCategory VerticalCategory { get; private set; }

    public DateTime Date { get; private set; }

    public int ReservationCount { get; private set; }

    public int TransferVolume { get; private set; }

    public int ParticipatingStoreCount { get; private set; }

    private DailyMarketSignal()
    {
    }

    public DailyMarketSignal(
        Guid productId,
        VerticalCategory verticalCategory,
        DateTime date)
    {
        ProductId = productId;
        VerticalCategory = verticalCategory;
        Date = date;
    }

    public void Update(
        int reservationCount,
        int transferVolume,
        int participatingStoreCount)
    {
        ReservationCount = reservationCount;
        TransferVolume = transferVolume;
        ParticipatingStoreCount = participatingStoreCount;
    }
}