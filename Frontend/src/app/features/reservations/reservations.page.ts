import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state';
import { Reservation } from '../../core/models/reservation';
import { RESERVATION_STATUSES } from '../../core/models/enums';
import { ReservationService } from '../../core/services/reservation.service';
import { ToastService } from '../../core/services/toast.service';
import { ConfirmService } from '../../shared/confirm-dialog/confirm.helper';
import { firstDetail } from '../../core/interceptors/api-error';
import { formatCurrency, formatDateTime } from '../../core/utils/format';

@Component({
  selector: 'app-reservations',
  imports: [
    MatCardModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatIconModule,
    MatSelectModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    PageHeaderComponent,
    EmptyStateComponent,
  ],
  templateUrl: './reservations.page.html',
  styleUrl: './reservations.page.scss',
})
export class ReservationsPage implements OnInit {
  private readonly reservations = inject(ReservationService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);
  private readonly route = inject(ActivatedRoute);

  readonly statuses = RESERVATION_STATUSES;

  readonly loading = signal(false);
  readonly error = signal('');
  readonly items = signal<Reservation[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(20);

  readonly incoming = signal(true);
  readonly status = signal('');

  readonly pendingCount = signal(0);

  readonly formatCurrency = formatCurrency;
  readonly formatDateTime = formatDateTime;

  async ngOnInit(): Promise<void> {
    const payment = this.route.snapshot.queryParamMap.get('payment');
    if (payment === 'success') {
      this.toast.success('Payment confirmed — the transfer is now complete.');
    } else if (payment === 'cancelled') {
      this.toast.success('Checkout cancelled — you can pay again from the inbox.');
    }
    await this.load();
    await this.refreshPendingCount();
  }

  private async refreshPendingCount(): Promise<void> {
    try {
      const result = await this.reservations.list({ status: 'Pending', pageSize: 1 });
      this.pendingCount.set(result.totalCount);
    } catch {
      this.pendingCount.set(0);
    }
  }

  async onDirectionChange(direction: boolean): Promise<void> {
    this.incoming.set(direction);
    this.page.set(1);
    await this.load();
  }

  async onStatusChange(): Promise<void> {
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
      const result = await this.reservations.list({
        incoming: this.incoming(),
        status: this.status(),
        page: this.page(),
        pageSize: this.pageSize(),
      });
      this.items.set(result.items);
      this.total.set(result.totalCount);
    } catch (error) {
      this.error.set(firstDetail(error));
    } finally {
      this.loading.set(false);
    }
  }

  async resolve(item: Reservation, action: 'Accepted' | 'Cancelled'): Promise<void> {
    const actionLabel = action === 'Accepted' ? 'Accept and ship' : 'Cancel';
    const confirmed = await this.confirm.confirm({
      title: `Reservation #${item.id.slice(0, 8)}`,
      message:
        action === 'Accepted'
          ? `Accept the request for ${item.quantity} × ${item.productName} from ${item.requestingStoreName}? You commit to ship it; payment follows delivery.`
          : item.status === 'Accepted'
            ? `Cancel this accepted reservation? The held quantity returns to your shared pool.`
            : `Reject this reservation from ${item.requestingStoreName}?`,
      confirmLabel: actionLabel,
    });

    if (!confirmed) {
      return;
    }

    try {
      await this.reservations.resolve(item.id, action);
      this.toast.success(
        action === 'Accepted'
          ? 'Reservation accepted — ship the goods, payment follows delivery.'
          : 'Reservation cancelled.',
      );
      await this.load();
      await this.refreshPendingCount();
    } catch (error) {
      this.toast.error(firstDetail(error));
    }
  }

  async pay(item: Reservation): Promise<void> {
    try {
      const session = await this.reservations.createCheckout(item.id);
      window.location.href = session.checkoutUrl;
    } catch (error) {
      this.toast.error(firstDetail(error));
    }
  }
}