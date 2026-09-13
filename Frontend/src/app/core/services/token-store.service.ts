import { Injectable, signal } from '@angular/core';
import { decodeJwt, JwtPayload } from '../auth/jwt';

const ACCESS_TOKEN_KEY = 'stockmesh.access_token';
const REFRESH_TOKEN_KEY = 'stockmesh.refresh_token';

@Injectable({ providedIn: 'root' })
export class TokenStoreService {
  readonly accessToken = signal<string | null>(this.read(ACCESS_TOKEN_KEY));
  readonly refreshToken = signal<string | null>(this.read(REFRESH_TOKEN_KEY));

  setTokens(accessToken: string, refreshToken: string): void {
    this.write(ACCESS_TOKEN_KEY, accessToken);
    this.write(REFRESH_TOKEN_KEY, refreshToken);
    this.accessToken.set(accessToken);
    this.refreshToken.set(refreshToken);
  }

  clear(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    this.accessToken.set(null);
    this.refreshToken.set(null);
  }

  claims(): JwtPayload {
    const token = this.accessToken();
    return token ? decodeJwt(token) : {};
  }

  private read(key: string): string | null {
    return localStorage.getItem(key);
  }

  private write(key: string, value: string): void {
    localStorage.setItem(key, value);
  }
}