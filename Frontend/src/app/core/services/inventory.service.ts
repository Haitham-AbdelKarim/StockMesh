import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { lastValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Paginated } from '../models/paginated';
import {
  AddInventoryBatchRequest,
  InventoryBatch,
  ProductStockSummary,
  SetBatchSharingRequest,
  UpdateInventoryBatchRequest,
} from '../models/inventory';
import { buildParams } from './catalog.service';

@Injectable({ providedIn: 'root' })
export class InventoryService {
  private readonly http = inject(HttpClient);

  private get base(): string {
    return `${environment.apiBase}/api/v1/inventory`;
  }

  getBatches(query: { productId?: string; page?: number; pageSize?: number }): Promise<Paginated<InventoryBatch>> {
    return lastValueFrom(
      this.http.get<Paginated<InventoryBatch>>(`${this.base}/batches`, {
        params: buildParams({
          productId: query.productId ?? '',
          page: query.page ?? 1,
          pageSize: query.pageSize ?? 20,
        }),
      }),
    );
  }

  getBatch(batchId: string): Promise<InventoryBatch> {
    return lastValueFrom(this.http.get<InventoryBatch>(`${this.base}/batches/${batchId}`));
  }

  addBatch(body: AddInventoryBatchRequest): Promise<InventoryBatch> {
    return lastValueFrom(this.http.post<InventoryBatch>(`${this.base}/batches`, body));
  }

  updateBatch(batchId: string, body: UpdateInventoryBatchRequest): Promise<InventoryBatch> {
    return lastValueFrom(this.http.patch<InventoryBatch>(`${this.base}/batches/${batchId}`, body));
  }

  setSharing(batchId: string, body: SetBatchSharingRequest): Promise<InventoryBatch> {
    return lastValueFrom(this.http.patch<InventoryBatch>(`${this.base}/batches/${batchId}/sharing`, body));
  }

  getStock(query: { page?: number; pageSize?: number }): Promise<Paginated<ProductStockSummary>> {
    return lastValueFrom(
      this.http.get<Paginated<ProductStockSummary>>(`${this.base}/stock`, {
        params: buildParams({ page: query.page ?? 1, pageSize: query.pageSize ?? 20 }),
      }),
    );
  }
}