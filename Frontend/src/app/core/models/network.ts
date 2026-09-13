export interface NetworkListing {
  batchId: string;
  storeId: string;
  productId: string;
  productName: string;
  storeName: string;
  distanceKm: number;
  sharedQuantity: number;
  unitSalePrice: number;
  expiryDate: string | null;
}