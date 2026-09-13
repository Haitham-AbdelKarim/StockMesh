import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { firstValueFrom } from 'rxjs';
import { TokenResponse } from '../models/auth';
import { AuthService } from '../services/auth.service';
import { TokenStoreService } from '../services/token-store.service';

let refreshFlow: Promise<TokenResponse | null> | null = null;

async function refreshTokenFlow(auth: AuthService): Promise<TokenResponse | null> {
  if (!refreshFlow) {
    refreshFlow = firstValueFrom(auth.refresh())
      .catch(() => {
        auth.clearSession();
        return null;
      })
      .finally(() => {
        refreshFlow = null;
      });
  }

  return refreshFlow;
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokenStore = inject(TokenStoreService);
  const auth = inject(AuthService);
  const router = inject(Router);

  const isAuthCall = req.url.includes('/auth/');

  if (isAuthCall) {
    return next(req);
  }

  const token = tokenStore.accessToken();
  const requestWithToken = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(requestWithToken).pipe(
    catchError((error) => {
      if (error.status === 401) {
        return from(refreshTokenFlow(auth)).pipe(
          switchMap((tokens) => {
            if (!tokens) {
              router.navigate(['/auth/login']);
              return throwError(() => error);
            }

            auth.persistTokens(tokens);

            const retried = req.clone({
              setHeaders: { Authorization: `Bearer ${tokens.accessToken}` },
            });
            return next(retried);
          }),
        );
      }

      return throwError(() => error);
    }),
  );
};