import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { lastValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  DashboardSummary,
  LowStockItem,
  NetworkSummary,
  SalesTrendPoint,
  SlowMover,
  TopSeller,
} from '../models/dashboard';
import { buildParams } from './catalog.service';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);

  private get base(): string {
    return `${environment.apiBase}/api/v1/dashboard`;
  }

  summary(date?: string): Promise<DashboardSummary> {
    return lastValueFrom(
      this.http.get<DashboardSummary>(`${this.base}/summary`, {
        params: buildParams({ date: date ?? '' }),
      }),
    );
  }

  salesTrend(days: number): Promise<SalesTrendPoint[]> {
    return lastValueFrom(
      this.http.get<SalesTrendPoint[]>(`${this.base}/sales-trend`, {
        params: buildParams({ days }),
      }),
    );
  }

  topSellers(query?: { topN?: number; from?: string; to?: string }): Promise<TopSeller[]> {
    return lastValueFrom(
      this.http.get<TopSeller[]>(`${this.base}/top-sellers`, {
        params: buildParams({
          topN: query?.topN ?? 5,
          from: query?.from ?? '',
          to: query?.to ?? '',
        }),
      }),
    );
  }

  slowMovers(days: number): Promise<SlowMover[]> {
    return lastValueFrom(
      this.http.get<SlowMover[]>(`${this.base}/slow-movers`, {
        params: buildParams({ days }),
      }),
    );
  }

  network(days: number): Promise<NetworkSummary> {
    return lastValueFrom(
      this.http.get<NetworkSummary>(`${this.base}/network`, {
        params: buildParams({ days }),
      }),
    );
  }

  lowStock(): Promise<LowStockItem[]> {
    return lastValueFrom(this.http.get<LowStockItem[]>(`${this.base}/low-stock`));
  }
}