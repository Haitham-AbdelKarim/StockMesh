import { Component, OnInit, computed, inject, signal } from '@angular/core';
import {
  MAT_DIALOG_DATA,
  MatDialog,
  MatDialogModule,
  MatDialogRef,
} from '@angular/material/dialog';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state';
import {
  InventoryBatch,
  ProductStockSummary,
  SetBatchSharingRequest,
} from '../../core/models/inventory';
import { Recommendation } from '../../core/models/recommendation';
import { recommendationActionClass } from '../../core/utils/recommendations';
import { InventoryService } from '../../core/services/inventory.service';
import { RecommendationService } from '../../core/services/recommendation.service';
import { ToastService } from '../../core/services/toast.service';
import { firstDetail } from '../../core/interceptors/api-error';
import { formatCurrency, formatDate } from '../../core/utils/format';
import { fetchAllPages } from '../../core/utils/pagination';
import { ConfirmService } from '../../shared/confirm-dialog/confirm.helper';

@Component({
  selector: 'app-batch-form-dialog',
  imports: [
    FormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
  ],
  template: `
    <h2 mat-dialog-title>Edit batch</h2>
    <mat-dialog-content class="dialog-content">
      <form #form="ngForm">
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Unit sale price</mat-label>
          <input matInput type="number" min="0.01" step="0.01" [(ngModel)]="unitSalePrice" name="unitSalePrice" required />
        </mat-form-field>

        <div class="grid grid-cols-2 gap-4">
            <mat-form-field appearance="outline">
              <mat-label>Reorder point</mat-label>
              <input matInput type="number" min="0" [(ngModel)]="reorderPoint" name="reorderPoint" required />
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Lead time (days)</mat-label>
              <input matInput type="number" min="0" [(ngModel)]="leadTimeDays" name="leadTimeDays" required />
            </mat-form-field>
          </div>

        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Expiry date</mat-label>
          <input matInput type="date" [(ngModel)]="expiryDate" name="expiryDate" />
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid || submitting()" (click)="save()">
        {{ submitting() ? 'Saving…' : 'Save' }}
      </button>
    </mat-dialog-actions>
  `,
})
export class BatchFormDialogComponent {
  private readonly dialogRef = inject(MatDialogRef<BatchFormDialogComponent>);
  private readonly inventory = inject(InventoryService);
  private readonly toast = inject(ToastService);
  private readonly data = inject<BatchFormData>(MAT_DIALOG_DATA);

  readonly submitting = signal(false);

  unitSalePrice: number | null = this.data.batch.unitSalePrice ?? null;
  reorderPoint: number | null = this.data.batch.reorderPoint ?? null;
  leadTimeDays: number | null = this.data.batch.leadTimeDays ?? null;
  expiryDate: string | null = this.data.batch.expiryDate?.slice(0, 10) ?? null;

  async save(): Promise<void> {
    this.submitting.set(true);
    try {
      await this.inventory.updateBatch(this.data.batch.id, {
        unitSalePrice: this.unitSalePrice ? Number(this.unitSalePrice) : null,
        reorderPoint: this.reorderPoint ? Number(this.reorderPoint) : null,
        leadTimeDays: this.leadTimeDays !== null ? Number(this.leadTimeDays) : null,
        expiryDate: this.expiryDate ? new Date(this.expiryDate).toISOString() : null,
      });
      this.toast.success('Batch updated.');
      this.dialogRef.close(true);
    } catch (error) {
      this.toast.error(firstDetail(error));
    } finally {
      this.submitting.set(false);
    }
  }
}

export interface BatchFormData {
  batch: InventoryBatch;
}

@Component({
  selector: 'app-share-batch-dialog',
  imports: [FormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  template: `
    <h2 mat-dialog-title>{{ shared() ? 'Manage network sharing' : 'Share to network' }}</h2>
    <mat-dialog-content class="dialog-content">
      @if (shared()) {
        <div class="mb-4 grid grid-cols-3 gap-3 text-center">
          <div class="rounded-xl bg-surface-container-low p-3">
            <div class="text-xl font-semibold">{{ batch.quantityRemaining }}</div>
            <div class="text-xs text-on-surface-variant">Local</div>
          </div>
          <div class="rounded-xl bg-surface-container-low p-3">
            <div class="text-xl font-semibold">{{ batch.sharedQuantity }}</div>
            <div class="text-xs text-on-surface-variant">Network</div>
          </div>
          <div class="rounded-xl bg-surface-container-low p-3">
            <div class="text-xl font-semibold">{{ total() }}</div>
            <div class="text-xs text-on-surface-variant">Total</div>
          </div>
        </div>
        <p class="m-0 mb-4 text-sm text-on-surface-variant">
          Set how many units stay exposed to partner stores. Lowering the amount returns units to local stock.
        </p>
      } @else {
        <p class="m-0 mb-4 text-sm text-on-surface-variant">
          Expose part of this batch to partner stores so they can reserve and purchase stock.
        </p>
      }

      <mat-form-field appearance="outline" class="w-full">
        <mat-label>Shared quantity</mat-label>
        <input matInput type="number" min="0" [max]="total()" [(ngModel)]="sharedQuantity" name="sharedQuantity" required />
      </mat-form-field>
      <p class="m-0 text-xs text-on-surface-variant">
        Available to share: {{ total() }} units ({{ batch.quantityRemaining }} local · {{ batch.sharedQuantity }} network)
      </p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      @if (shared()) {
        <button mat-stroked-button color="warn" [disabled]="submitting()" (click)="stopSharing()">Stop sharing</button>
      }
      <button
        mat-flat-button
        color="primary"
        [disabled]="submitting() || sharedQuantity === null || sharedQuantity < 0 || sharedQuantity > total()"
        (click)="save()"
      >
        {{ submitting() ? 'Saving…' : shared() ? 'Update' : 'Share' }}
      </button>
    </mat-dialog-actions>
  `,
})
export class ShareBatchDialogComponent {
  private readonly dialogRef = inject(MatDialogRef<ShareBatchDialogComponent>);
  private readonly inventory = inject(InventoryService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);

  private readonly data = inject<ShareBatchData>(MAT_DIALOG_DATA);
  readonly batch = this.data.batch;
  readonly shared = signal(this.data.batch.isShared);
  readonly submitting = signal(false);
  readonly total = computed(() => this.batch.quantityRemaining + this.batch.sharedQuantity);
  sharedQuantity: number | null = this.data.batch.isShared
    ? this.data.batch.sharedQuantity
    : this.data.batch.quantityRemaining;

  async save(): Promise<void> {
    const target = Number(this.sharedQuantity);
    const current = this.batch.sharedQuantity;

    if (target === current) {
      this.dialogRef.close(false);
      return;
    }

    this.submitting.set(true);
    try {
      const body: SetBatchSharingRequest = { sharedQuantity: target };
      await this.inventory.setSharing(this.batch.id, body);
      this.toast.success(target > current ? 'Shared quantity increased.' : 'Shared quantity reduced.');
      this.dialogRef.close(true);
    } catch (error) {
      this.toast.error(firstDetail(error));
    } finally {
      this.submitting.set(false);
    }
  }

  async stopSharing(): Promise<void> {
    const confirmed = await this.confirm.confirm({
      title: 'Stop sharing this batch?',
      message: `All ${this.batch.sharedQuantity} shared units will return to local stock and the batch will leave the network.`,
      confirmLabel: 'Stop sharing',
    });
    if (!confirmed) {
      return;
    }

    this.submitting.set(true);
    try {
      await this.inventory.setSharing(this.batch.id, { sharedQuantity: 0 });
      this.toast.success('Batch removed from the network.');
      this.dialogRef.close(true);
    } catch (error) {
      this.toast.error(firstDetail(error));
    } finally {
      this.submitting.set(false);
    }
  }
}

export interface ShareBatchData {
  batch: InventoryBatch;
}

@Component({
  selector: 'app-inventory',
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTabsModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatInputModule,
    MatTooltipModule,
    PageHeaderComponent,
    EmptyStateComponent,
  ],
  templateUrl: './inventory.page.html',
  styleUrl: './inventory.page.scss',
})
export class InventoryPage implements OnInit {
  private readonly inventory = inject(InventoryService);
  private readonly recommendations = inject(RecommendationService);
  private readonly dialog = inject(MatDialog);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);

  readonly loading = signal(false);
  readonly error = signal('');
  readonly batches = signal<InventoryBatch[]>([]);
  readonly stock = signal<ProductStockSummary[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly productFilter = signal('');
  readonly batchAdvice = signal<Map<string, Recommendation>>(new Map());

  readonly formatCurrency = formatCurrency;
  readonly formatDate = formatDate;
  readonly actionClass = recommendationActionClass;

  adviceFor(batchId: string): Recommendation | undefined {
    return this.batchAdvice().get(batchId);
  }

  async ngOnInit(): Promise<void> {
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
      const [batchResult, stockItems] = await Promise.all([
        this.inventory.getBatches({
          productId: this.productFilter(),
          page: this.page(),
          pageSize: this.pageSize(),
        }),
        fetchAllPages((page, pageSize) => this.inventory.getStock({ page, pageSize })),
        this.loadBatchAdvice(),
      ]);
      this.batches.set(batchResult.items);
      this.total.set(batchResult.totalCount);
      this.stock.set(stockItems);
    } catch (error) {
      this.error.set(firstDetail(error));
    } finally {
      this.loading.set(false);
    }
  }

  private async loadBatchAdvice(): Promise<void> {
    try {
      const result = await this.recommendations.list({ page: 1, pageSize: 100 });
      const advice = new Map<string, Recommendation>();
      for (const item of result.items) {
        if (
          item.batchId &&
          (item.recommendedAction === 'Share' || item.recommendedAction === 'UrgentShare') &&
          !advice.has(item.batchId)
        ) {
          advice.set(item.batchId, item);
        }
      }
      this.batchAdvice.set(advice);
    } catch {
      this.batchAdvice.set(new Map());
    }
  }

  async openEditBatch(batch: InventoryBatch): Promise<void> {
    const dialogRef = this.dialog.open(BatchFormDialogComponent, {
      width: '480px',
      data: { batch } as BatchFormData,
    });
    dialogRef.afterClosed().subscribe(async (saved) => {
      if (saved) {
        await this.load();
      }
    });
  }

  openShareBatch(batch: InventoryBatch): void {
    const dialogRef = this.dialog.open(ShareBatchDialogComponent, {
      width: '420px',
      data: { batch } as ShareBatchData,
    });
    dialogRef.afterClosed().subscribe(async (saved) => {
      if (saved) {
        await this.load();
      }
    });
  }

  async confirmUnshareBatch(batch: InventoryBatch): Promise<void> {
    const confirmed = await this.confirm.confirm({
      title: 'Stop sharing this batch?',
      message: `All ${batch.sharedQuantity} shared units will return to local stock and the batch will leave the network.`,
      confirmLabel: 'Stop sharing',
    });
    if (confirmed) {
      await this.unshareBatch(batch);
    }
  }

  async unshareBatch(batch: InventoryBatch): Promise<void> {
    try {
      await this.inventory.setSharing(batch.id, { sharedQuantity: 0 });
      this.toast.info('Batch removed from the network.');
      await this.load();
    } catch (error) {
      this.toast.error(firstDetail(error));
    }
  }
}