import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { lastValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Paginated } from '../models/paginated';
import { RecordRestockRequest, RecordSaleRequest, RestockResponse, SaleResponse, StockMovement } from '../models/movement';
import { buildParams } from './catalog.service';

@Injectable({ providedIn: 'root' })
export class StockService {
  private readonly http = inject(HttpClient);

  private get base(): string {
    return `${environment.apiBase}/api/v1`;
  }

  recordSale(body: RecordSaleRequest): Promise<SaleResponse> {
    return lastValueFrom(this.http.post<SaleResponse>(`${this.base}/sales`, body));
  }

  recordRestock(body: RecordRestockRequest): Promise<RestockResponse> {
    return lastValueFrom(this.http.post<RestockResponse>(`${this.base}/restocks`, body));
  }

  getMovements(query: {
    movementType?: string;
    from?: string;
    to?: string;
    relatedStoreId?: string;
    page?: number;
    pageSize?: number;
  }): Promise<Paginated<StockMovement>> {
    return lastValueFrom(
      this.http.get<Paginated<StockMovement>>(`${this.base}/stock-movements`, {
        params: buildParams({
          movementType: query.movementType ?? '',
          from: query.from ?? '',
          to: query.to ?? '',
          relatedStoreId: query.relatedStoreId ?? '',
          page: query.page ?? 1,
          pageSize: query.pageSize ?? 20,
        }),
      }),
    );
  }
}