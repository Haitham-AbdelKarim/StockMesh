using Domain.Common;

namespace Domain.Entities;

public class DailyProductMetric : BaseEntity
{
    public Guid StoreId { get; private set; }

    public Guid ProductId { get; private set; }

    public DateTime Date { get; private set; }

    public int UnitsSold { get; private set; }

    public decimal SalesRevenue { get; private set; }

    public decimal SalesCost { get; private set; }

    public int TransfersOutUnits { get; private set; }

    public decimal TransfersOutRevenue { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    private DailyProductMetric()
    {
    }

    public DailyProductMetric(Guid storeId, Guid productId, DateTime date)
    {
        StoreId = storeId;
        ProductId = productId;
        Date = date;
    }

    public void Update(
        int unitsSold,
        decimal salesRevenue,
        decimal salesCost,
        int transfersOutUnits,
        decimal transfersOutRevenue)
    {
        UnitsSold = unitsSold;
        SalesRevenue = salesRevenue;
        SalesCost = salesCost;
        TransfersOutUnits = transfersOutUnits;
        TransfersOutRevenue = transfersOutRevenue;
    }
}