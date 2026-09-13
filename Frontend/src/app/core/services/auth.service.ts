import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { lastValueFrom, Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { JoinStoreResponse, RegisterStoreRequest, RegisterStoreResponse, TokenResponse } from '../models/auth';
import { StoreProfile } from '../models/store';
import { JwtPayload } from '../auth/jwt';
import { TokenStoreService } from './token-store.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokenStore = inject(TokenStoreService);

  private readonly profile = signal<StoreProfile | null>(null);

  readonly isAuthenticated = computed(() => this.tokenStore.accessToken() !== null);
  readonly claims = computed<JwtPayload>(() => this.tokenStore.claims());
  readonly storeId = computed(() => this.claims().store_id ?? '');
  readonly role = computed(() => this.claims().role ?? '');
  readonly email = computed(() => this.claims().email ?? '');
  readonly verticalCategory = computed(() => this.claims().vertical_category ?? '');

  readonly storeProfile = this.profile.asReadonly();
  readonly isOwner = computed(() => this.role() === 'Owner');

  private get base(): string {
    return `${environment.apiBase}/api/v1/auth`;
  }

  login(email: string, password: string): Promise<TokenResponse> {
    return lastValueFrom(this.http.post<TokenResponse>(`${this.base}/login`, { email, password }));
  }

  register(payload: RegisterStoreRequest): Promise<RegisterStoreResponse> {
    return lastValueFrom(this.http.post<RegisterStoreResponse>(`${this.base}/register-store`, payload));
  }

  join(email: string, password: string): Promise<JoinStoreResponse> {
    return lastValueFrom(this.http.post<JoinStoreResponse>(`${this.base}/join-store`, { email, password }));
  }

  refresh(): Observable<TokenResponse> {
    const refreshToken = this.tokenStore.refreshToken();
    return this.http.post<TokenResponse>(`${this.base}/refresh`, { refreshToken });
  }

  logout(): Promise<void> {
    const refreshToken = this.tokenStore.refreshToken();
    if (!refreshToken) {
      return Promise.resolve();
    }
    return lastValueFrom(this.http.post<void>(`${this.base}/logout`, { refreshToken })).then(
      () => undefined,
      () => undefined,
    );
  }

  persistTokens(tokens: TokenResponse): void {
    this.tokenStore.setTokens(tokens.accessToken, tokens.refreshToken);
  }

  async signIn(email: string, password: string): Promise<void> {
    const tokens = await this.login(email, password);
    this.persistTokens(tokens);
  }

  clearSession(): void {
    this.tokenStore.clear();
    this.profile.set(null);
  }

  async loadProfile(): Promise<StoreProfile | null> {
    if (!this.isAuthenticated()) {
      return null;
    }

    try {
      const profile = await lastValueFrom(
        this.http.get<StoreProfile>(`${environment.apiBase}/api/v1/stores/me`),
      );
      this.profile.set(profile);
      return profile;
    } catch {
      return null;
    }
  }
}