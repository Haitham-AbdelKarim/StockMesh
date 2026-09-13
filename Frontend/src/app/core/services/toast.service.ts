import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly snackBar = inject(MatSnackBar);

  success(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 3500,
      panelClass: ['toast-success'],
      horizontalPosition: 'right',
      verticalPosition: 'top',
    });
  }

  error(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 6000,
      panelClass: ['toast-error'],
      horizontalPosition: 'right',
      verticalPosition: 'top',
    });
  }

  info(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 4000,
      panelClass: ['toast-info'],
      horizontalPosition: 'right',
      verticalPosition: 'top',
    });
  }
}