using Domain.Common;

namespace Domain.Entities;

public class DailyStoreMetric : BaseEntity
{
    public Guid StoreId { get; private set; }

    public DateTime Date { get; private set; }

    public decimal SalesRevenue { get; private set; }

    public decimal TransfersOutRevenue { get; private set; }

    public decimal CostOfGoodsSold { get; private set; }

    public decimal StockPurchases { get; private set; }

    public decimal ExpenseTotal { get; private set; }

    public decimal NetProfit { get; private set; }

    public int UnitsSold { get; private set; }

    public int TransfersOutUnits { get; private set; }

    public int TransfersInUnits { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    private DailyStoreMetric()
    {
    }

    public DailyStoreMetric(Guid storeId, DateTime date)
    {
        StoreId = storeId;
        Date = date;
    }

    public void Update(
        decimal salesRevenue,
        decimal transfersOutRevenue,
        decimal costOfGoodsSold,
        decimal stockPurchases,
        decimal expenseTotal,
        int unitsSold,
        int transfersOutUnits,
        int transfersInUnits)
    {
        SalesRevenue = salesRevenue;
        TransfersOutRevenue = transfersOutRevenue;
        CostOfGoodsSold = costOfGoodsSold;
        StockPurchases = stockPurchases;
        ExpenseTotal = expenseTotal;
        UnitsSold = unitsSold;
        TransfersOutUnits = transfersOutUnits;
        TransfersInUnits = transfersInUnits;
        NetProfit = salesRevenue + transfersOutRevenue - costOfGoodsSold - stockPurchases - expenseTotal;
    }
}