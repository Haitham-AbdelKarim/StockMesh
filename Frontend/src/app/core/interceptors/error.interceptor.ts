import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ApiError, extractError } from './api-error';
import { ToastService } from '../services/toast.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);

  return next(req).pipe(
    catchError((error) => {
      const parsed = extractError(error);

      if (parsed.status >= 400 && parsed.status < 500 && parsed.status !== 401 && parsed.status !== 422) {
        toast.error(parsed.message);
      }

      if (parsed.status === 500) {
        toast.error('Something went wrong on the server. Please try again.');
      }

      return throwError(() => new ApiError(error));
    }),
  );
};