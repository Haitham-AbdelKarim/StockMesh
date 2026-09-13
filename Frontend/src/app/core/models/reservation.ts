export interface Reservation {
  id: string;
  batchId: string;
  productId: string;
  productName: string;
  requestingStoreId: string;
  requestingStoreName: string;
  owningStoreId: string;
  owningStoreName: string;
  quantity: number;
  unitPrice: number;
  distanceKm: number | null;
  deliveryEta: string | null;
  status: string;
  holdExpiresAt: string;
  resolvedAt: string | null;
  paymentState: 'Unpaid' | 'Pending' | 'Paid' | 'Failed';
}

export interface ReserveStockRequest {
  batchId: string;
  quantity: number;
  distanceKm?: number | null;
  deliveryEta?: string | null;
}

export interface ResolveReservationRequest {
  outcome: 'Accepted' | 'Cancelled';
}

export interface CheckoutSession {
  sessionId: string;
  checkoutUrl: string;
}