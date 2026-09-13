import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { lastValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Paginated } from '../models/paginated';
import { MarketSignalPoint, Recommendation, RecommendedAction, RunSummary } from '../models/recommendation';
import { buildParams } from './catalog.service';

@Injectable({ providedIn: 'root' })
export class RecommendationService {
  private readonly http = inject(HttpClient);

  private get base(): string {
    return `${environment.apiBase}/api/v1/recommendations`;
  }

  list(query: {
    recommendedAction?: RecommendedAction | '';
    page?: number;
    pageSize?: number;
  }): Promise<Paginated<Recommendation>> {
    return lastValueFrom(
      this.http.get<Paginated<Recommendation>>(`${this.base}`, {
        params: buildParams({
          recommendedAction: query.recommendedAction ?? '',
          page: query.page ?? 1,
          pageSize: query.pageSize ?? 20,
        }),
      }),
    );
  }

  run(): Promise<RunSummary> {
    return lastValueFrom(this.http.post<RunSummary>(`${this.base}/run`, {}));
  }

  marketHistory(productId: string, days = 90): Promise<MarketSignalPoint[]> {
    return lastValueFrom(
      this.http.get<MarketSignalPoint[]>(`${this.base}/market`, {
        params: buildParams({ productId, days }),
      }),
    );
  }
}
