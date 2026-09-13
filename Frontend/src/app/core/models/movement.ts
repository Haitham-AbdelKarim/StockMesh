export interface StockMovement {
  id: string;
  storeId: string;
  batchId: string;
  relatedStoreId: string | null;
  movementType: string;
  quantity: number;
  unitPrice: number | null;
  unitCost: number | null;
  supplierName: string | null;
  note: string | null;
  occurredAt: string;
  productId: string;
  productName: string;
}

export interface RecordSaleRequest {
  batchId: string;
  quantity: number;
}

export interface SaleResponse {
  movementId: string;
  batchId: string;
  quantity: number;
  unitSalePrice: number;
  totalPrice: number;
}

export interface RecordRestockRequest {
  productId: string;
  quantity: number;
  unitCost: number;
  supplierName?: string | null;
  expiryDate?: string | null;
}

export interface RestockResponse {
  batchId: string;
  productId: string;
  quantity: number;
  unitCost: number;
  unitSalePrice: number;
}