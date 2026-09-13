import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { lastValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ConnectOnboarding {
  accountId: string;
  onboardingUrl: string;
}

export interface ConnectStatus {
  accountId: string | null;
  payoutsEnabled: boolean;
}

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private readonly http = inject(HttpClient);

  private get base(): string {
    return `${environment.apiBase}/api/v1/payments`;
  }

  onboard(): Promise<ConnectOnboarding> {
    return lastValueFrom(this.http.post<ConnectOnboarding>(`${this.base}/connect/onboard`, {}));
  }

  connectStatus(): Promise<ConnectStatus> {
    return lastValueFrom(this.http.get<ConnectStatus>(`${this.base}/connect/status`));
  }
}
