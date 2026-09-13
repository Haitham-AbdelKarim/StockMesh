export type RecommendedAction = 'Hold' | 'Reorder' | 'Share' | 'UrgentShare' | 'MarketOpportunity';

export interface Recommendation {
  id: string;
  storeId: string;
  productId: string;
  productName: string;
  batchId: string | null;
  recommendedAction: RecommendedAction;
  trendSlope: number;
  anomalyDetected: boolean;
  predictedDepletionDate: string | null;
  confidenceScore: number;
  modelVersion: string;
  reason: string;
  generatedAt: string;
  forecast: ForecastSnapshot | null;
}

export interface ForecastSnapshot {
  dates: string[];
  actuals: number[];
  yhat: number[];
  lower: number[];
  upper: number[];
}

export interface MarketSignalPoint {
  date: string;
  reservationCount: number;
  transferVolume: number;
  participatingStoreCount: number;
}

export interface RunSummary {
  productsEvaluated: number;
  recommendationsWritten: number;
  skipped: number;
  failed: number;
}
