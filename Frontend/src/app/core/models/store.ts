export interface StoreProfile {
  storeId: string;
  name: string;
  verticalCategory: string;
  latitude: number;
  longitude: number;
  maxSearchRadiusKm: number;
  isVerified: boolean;
}

export interface UpdateStoreProfileRequest {
  name: string;
  latitude: number;
  longitude: number;
  maxSearchRadiusKm: number;
}