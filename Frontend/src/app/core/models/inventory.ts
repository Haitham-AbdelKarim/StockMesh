export interface InventoryBatch {
  id: string;
  storeId: string;
  productId: string;
  productName: string;
  quantityRemaining: number;
  sharedQuantity: number;
  isShared: boolean;
  unitCost: number;
  unitSalePrice: number;
  reorderPoint: number;
  leadTimeDays: number;
  expiryDate: string | null;
  receivedAt: string;
}

export interface AddInventoryBatchRequest {
  productId: string;
  quantity: number;
  unitCost: number;
  unitSalePrice?: number | null;
  expiryDate?: string | null;
}

export interface UpdateInventoryBatchRequest {
  unitSalePrice?: number | null;
  reorderPoint?: number | null;
  leadTimeDays?: number | null;
  expiryDate?: string | null;
}

export interface SetBatchSharingRequest {
  sharedQuantity: number;
}

export interface ProductStockSummary {
  productId: string;
  productName: string;
  totalQuantityRemaining: number;
  totalSharedQuantity: number;
  batchCount: number;
}