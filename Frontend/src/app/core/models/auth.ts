export interface TokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export interface RegisterStoreResponse {
  storeId: string;
  userId: string;
  tokens: TokenResponse;
}

export interface JoinStoreResponse {
  userId: string;
  email: string;
}

export interface RegisterStoreRequest {
  storeName: string;
  verticalCategory: string;
  latitude: number;
  longitude: number;
  maxSearchRadiusKm: number;
  email: string;
  password: string;
}

export interface JoinStoreRequest {
  email: string;
  password: string;
}