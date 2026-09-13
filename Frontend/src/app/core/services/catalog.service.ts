import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { lastValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { StoreProfile, UpdateStoreProfileRequest } from '../models/store';
import { Paginated } from '../models/paginated';
import { Product } from '../models/product';
import { VERTICAL_CATEGORIES } from '../models/enums';

export function buildParams(query: Record<string, unknown>): HttpParams {
  let params = new HttpParams();

  Object.entries(query).forEach(([key, value]) => {
    if (value === undefined || value === null || value === '') {
      return;
    }

    params = params.set(key, String(value));
  });

  return params;
}

@Injectable({ providedIn: 'root' })
export class StoreService {
  private readonly http = inject(HttpClient);

  private get base(): string {
    return `${environment.apiBase}/api/v1/stores`;
  }

  getProfile(): Promise<StoreProfile> {
    return lastValueFrom(this.http.get<StoreProfile>(`${this.base}/me`));
  }

  updateProfile(body: UpdateStoreProfileRequest): Promise<StoreProfile> {
    return lastValueFrom(this.http.patch<StoreProfile>(`${this.base}/me`, body));
  }
}

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);

  getProducts(query: {
    vertical?: string;
    search?: string;
    page?: number;
    pageSize?: number;
  }): Promise<Paginated<Product>> {
    const params = buildParams({
      vertical: query.vertical ?? '',
      search: query.search ?? '',
      page: query.page ?? 1,
      pageSize: query.pageSize ?? 20,
    });

    return lastValueFrom(
      this.http.get<Paginated<Product>>(`${environment.apiBase}/api/v1/products`, { params }),
    );
  }

  verticalCategories(): readonly string[] {
    return VERTICAL_CATEGORIES;
  }
}