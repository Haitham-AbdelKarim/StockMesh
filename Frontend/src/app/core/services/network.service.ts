import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { lastValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Paginated } from '../models/paginated';
import { NetworkListing } from '../models/network';
import { buildParams } from './catalog.service';

@Injectable({ providedIn: 'root' })
export class NetworkService {
  private readonly http = inject(HttpClient);

  getListings(query: {
    category?: string;
    maxDistanceKm?: number;
    search?: string;
    page?: number;
    pageSize?: number;
  }): Promise<Paginated<NetworkListing>> {
    return lastValueFrom(
      this.http.get<Paginated<NetworkListing>>(`${environment.apiBase}/api/v1/network/listings`, {
        params: buildParams({
          category: query.category ?? '',
          maxDistanceKm: query.maxDistanceKm,
          search: query.search ?? '',
          page: query.page ?? 1,
          pageSize: query.pageSize ?? 20,
        }),
      }),
    );
  }
}