export interface Expense {
  id: string;
  storeId: string;
  category: string;
  amount: number;
  incurredAt: string;
  note: string | null;
}

export interface RecordExpenseRequest {
  category: string;
  amount: number;
  incurredAt: string;
  note?: string | null;
}