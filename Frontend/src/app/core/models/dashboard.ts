export interface DashboardSummary {
  date: string;
  salesRevenue: number;
  transfersOutRevenue: number;
  costOfGoodsSold: number;
  stockPurchases: number;
  expenseTotal: number;
  netProfit: number;
  unitsSold: number;
  transfersOutUnits: number;
  transfersInUnits: number;
}

export interface SalesTrendPoint {
  date: string;
  unitsSold: number;
  salesRevenue: number;
}

export interface TopSeller {
  productId: string;
  productName: string;
  unitsSold: number;
  salesRevenue: number;
}

export interface SlowMover {
  productId: string;
  productName: string;
  unitsSoldLast7Days: number;
  unitsSoldLastDays: number;
  quantityRemaining: number;
  estimatedDaysOfCover: number;
}

export interface NetworkSummary {
  transfersOutCount: number;
  transfersInCount: number;
  transfersOutUnits: number;
  transfersInUnits: number;
  transfersOutRevenue: number;
  transfersInValue: number;
  reservationSuccessRate: number;
}

export interface LowStockItem {
  productId: string;
  productName: string;
  totalQuantityRemaining: number;
  reorderPoint: number;
  leadTimeDays: number;
}