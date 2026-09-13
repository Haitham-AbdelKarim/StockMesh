import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import {
  MAT_DIALOG_DATA,
  MatDialog,
  MatDialogModule,
  MatDialogRef,
} from '@angular/material/dialog';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state';
import { NetworkListing } from '../../core/models/network';
import { ReserveStockRequest } from '../../core/models/reservation';
import { NetworkService } from '../../core/services/network.service';
import { ReservationService } from '../../core/services/reservation.service';
import { ProductService } from '../../core/services/catalog.service';
import { ToastService } from '../../core/services/toast.service';
import { firstDetail } from '../../core/interceptors/api-error';
import { formatCurrency, formatDate } from '../../core/utils/format';

@Component({
  selector: 'app-reserve-dialog',
  imports: [FormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  template: `
    <h2 mat-dialog-title>Reserve stock</h2>
    <mat-dialog-content class="dialog-content">
      <p class="m-0 mb-3 text-sm text-on-surface-variant">
        {{ data.listing.productName }} from <b>{{ data.listing.storeName }}</b> ·
        {{ data.listing.distanceKm.toFixed(1) }} km away · {{ formatCurrency(data.listing.unitSalePrice) }} each
      </p>

      <form #form="ngForm">
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Quantity</mat-label>
          <input matInput type="number" min="1" [max]="data.listing.sharedQuantity" [(ngModel)]="quantity" name="quantity" required />
        </mat-form-field>

        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Delivery ETA</mat-label>
          <input matInput type="date" [(ngModel)]="deliveryEta" name="deliveryEta" />
        </mat-form-field>
      </form>
      <p class="m-0 mt-2 text-xs text-on-surface-variant">
        Available to reserve: {{ data.listing.sharedQuantity }} units
      </p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid || submitting()" (click)="submit($event)">
        {{ submitting() ? 'Reserving…' : 'Reserve' }}
      </button>
    </mat-dialog-actions>
  `,
})
export class ReserveDialogComponent {
  private readonly dialogRef = inject(MatDialogRef<ReserveDialogComponent>);
  private readonly reservations = inject(ReservationService);
  private readonly toast = inject(ToastService);

  readonly submitting = signal(false);
  readonly formatCurrency = formatCurrency;

  quantity: number | null = 1;
  deliveryEta: string | null = null;

  readonly data = inject<{ listing: NetworkListing }>(MAT_DIALOG_DATA);

  async submit(event: Event): Promise<void> {
    event.preventDefault();
    this.submitting.set(true);

    try {
      const body: ReserveStockRequest = {
        batchId: this.data.listing.batchId,
        quantity: Number(this.quantity),
        distanceKm: this.data.listing.distanceKm,
        deliveryEta: this.deliveryEta ? new Date(this.deliveryEta).toISOString() : null,
      };
      await this.reservations.reserve(body);
      this.toast.success('Reservation requested. The owning store will confirm it.');
      this.dialogRef.close(true);
    } catch (error) {
      this.toast.error(firstDetail(error));
    } finally {
      this.submitting.set(false);
    }
  }
}

@Component({
  selector: 'app-network',
  imports: [
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    MatFormFieldModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    PageHeaderComponent,
    EmptyStateComponent,
  ],
  templateUrl: './network.page.html',
  styleUrl: './network.page.scss',
})
export class NetworkPage implements OnInit {
  private readonly network = inject(NetworkService);
  private readonly dialog = inject(MatDialog);

  readonly verticalCategories = inject(ProductService).verticalCategories();

  readonly loading = signal(false);
  readonly error = signal('');
  readonly listings = signal<NetworkListing[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(12);

  readonly category = signal('');
  readonly maxDistance = signal('');
  readonly search = signal('');

  readonly formatCurrency = formatCurrency;
  readonly formatDate = formatDate;

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  async onFilter(): Promise<void> {
    this.page.set(1);
    await this.load();
  }

  async onPage(event: PageEvent): Promise<void> {
    this.page.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
    await this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.error.set('');

    try {
      const result = await this.network.getListings({
        category: this.category(),
        maxDistanceKm: this.maxDistance() ? Number(this.maxDistance()) : undefined,
        search: this.search(),
        page: this.page(),
        pageSize: this.pageSize(),
      });
      this.listings.set(result.items);
      this.total.set(result.totalCount);
    } catch (error) {
      this.error.set(firstDetail(error));
    } finally {
      this.loading.set(false);
    }
  }

  openReserve(listing: NetworkListing): void {
    const ref = this.dialog.open(ReserveDialogComponent, {
      width: '440px',
      data: { listing },
    });
    ref.afterClosed().subscribe(async (saved) => {
      if (saved) {
        await this.load();
      }
    });
  }
}