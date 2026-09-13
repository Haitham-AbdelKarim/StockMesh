import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { lastValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Paginated } from '../models/paginated';
import { Reservation, ReserveStockRequest, ResolveReservationRequest, CheckoutSession } from '../models/reservation';
import { buildParams } from './catalog.service';

@Injectable({ providedIn: 'root' })
export class ReservationService {
  private readonly http = inject(HttpClient);

  private get base(): string {
    return `${environment.apiBase}/api/v1/reservations`;
  }

  list(query: {
    status?: string;
    incoming?: boolean;
    from?: string;
    to?: string;
    page?: number;
    pageSize?: number;
  }): Promise<Paginated<Reservation>> {
    return lastValueFrom(
      this.http.get<Paginated<Reservation>>(`${this.base}`, {
        params: buildParams({
          status: query.status ?? '',
          incoming: query.incoming,
          from: query.from ?? '',
          to: query.to ?? '',
          page: query.page ?? 1,
          pageSize: query.pageSize ?? 20,
        }),
      }),
    );
  }

  reserve(body: ReserveStockRequest): Promise<Reservation> {
    return lastValueFrom(this.http.post<Reservation>(`${this.base}`, body));
  }

  resolve(reservationId: string, outcome: 'Accepted' | 'Cancelled'): Promise<Reservation> {
    const body: ResolveReservationRequest = { outcome };
    return lastValueFrom(this.http.patch<Reservation>(`${this.base}/${reservationId}`, body));
  }

  createCheckout(reservationId: string): Promise<CheckoutSession> {
    return lastValueFrom(this.http.post<CheckoutSession>(`${this.base}/${reservationId}/checkout`, {}));
  }
}