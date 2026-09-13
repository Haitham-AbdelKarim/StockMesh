import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  MatDialog,
  MatDialogModule,
  MatDialogRef,
} from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { MatSelectModule } from '@angular/material/select';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state';
import {
  MOVEMENT_TYPES,
} from '../../core/models/enums';
import { InventoryBatch } from '../../core/models/inventory';
import {
  RecordRestockRequest,
  RecordSaleRequest,
  SaleResponse,
  StockMovement,
} from '../../core/models/movement';
import { Product } from '../../core/models/product';
import { InventoryService } from '../../core/services/inventory.service';
import { ProductService } from '../../core/services/catalog.service';
import { StockService } from '../../core/services/stock.service';
import { ToastService } from '../../core/services/toast.service';
import { firstDetail } from '../../core/interceptors/api-error';
import { formatCurrency, formatDateTime } from '../../core/utils/format';
import { fetchAllPages } from '../../core/utils/pagination';

@Component({
  selector: 'app-sale-dialog',
  imports: [FormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule],
  template: `
    <h2 mat-dialog-title>Record a sale</h2>
    <mat-dialog-content class="dialog-content">
      <form #form="ngForm">
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Batch</mat-label>
          <mat-select [(ngModel)]="batchId" name="batchId" required>
            @for (batch of batches(); track batch.id) {
              @if (batch.quantityRemaining > 0) {
                <mat-option [value]="batch.id">
                  {{ batch.productName }} · {{ batch.quantityRemaining }} in stock · {{ formatCurrency(batch.unitSalePrice) }}
                </mat-option>
              }
            }
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Quantity</mat-label>
          <input matInput type="number" min="1" [(ngModel)]="quantity" name="quantity" required />
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid || submitting()" (click)="submit($event)">
        {{ submitting() ? 'Selling…' : 'Record sale' }}
      </button>
    </mat-dialog-actions>
  `,
})
export class SaleDialogComponent implements OnInit {
  private readonly dialogRef = inject(MatDialogRef<SaleDialogComponent>);
  private readonly inventory = inject(InventoryService);
  private readonly stock = inject(StockService);
  private readonly toast = inject(ToastService);

  readonly batches = signal<InventoryBatch[]>([]);
  readonly submitting = signal(false);

  readonly formatCurrency = formatCurrency;

  batchId: string | null = null;
  quantity: number | null = null;

  async ngOnInit(): Promise<void> {
    this.batches.set(await fetchAllPages((page, pageSize) => this.inventory.getBatches({ page, pageSize })));
  }

  async submit(event: Event): Promise<void> {
    event.preventDefault();
    this.submitting.set(true);
    try {
      const body: RecordSaleRequest = {
        batchId: this.batchId!,
        quantity: Number(this.quantity),
      };
      const response: SaleResponse = await this.stock.recordSale(body);
      this.toast.success(`Sale recorded — ${formatCurrency(response.totalPrice)}.`);
      this.dialogRef.close(true);
    } catch (error) {
      this.toast.error(firstDetail(error));
    } finally {
      this.submitting.set(false);
    }
  }
}

@Component({
  selector: 'app-restock-dialog',
  imports: [FormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatAutocompleteModule],
  template: `
    <h2 mat-dialog-title>Record a restock</h2>
    <mat-dialog-content class="dialog-content">
      <form #form="ngForm">
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Product</mat-label>
          <input
            matInput
            type="text"
            name="productSearch"
            autocomplete="off"
            [value]="productSearchText()"
            [matAutocomplete]="productAuto"
            (input)="onProductInput($event)"
            required
          />
          <mat-autocomplete
            #productAuto="matAutocomplete"
            (optionSelected)="onProductSelected($event)"
            (opened)="onProductPanelOpened()"
            (closed)="onProductPanelClosed()"
          >
            @for (product of productOptions(); track product.id) {
              <mat-option [value]="product.id">{{ product.name }}</mat-option>
            }
            @if (loadingMoreProducts()) {
              <mat-option disabled>Loading more…</mat-option>
            } @else if (hasMoreProducts() && productOptions().length) {
              <mat-option disabled>Scroll for more ({{ productOptions().length }} of {{ productTotal() }})</mat-option>
            }
            @if (!loadingProducts() && !productOptions().length) {
              <mat-option disabled>No products found</mat-option>
            }
          </mat-autocomplete>
        </mat-form-field>

        <div class="grid grid-cols-2 gap-4">
          <mat-form-field appearance="outline">
            <mat-label>Quantity</mat-label>
            <input matInput type="number" min="1" [(ngModel)]="quantity" name="quantity" required />
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Unit cost</mat-label>
            <input matInput type="number" min="0.01" step="0.01" [(ngModel)]="unitCost" name="unitCost" required />
          </mat-form-field>
        </div>

        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Supplier</mat-label>
          <input matInput [(ngModel)]="supplierName" name="supplierName" />
        </mat-form-field>

        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Expiry date</mat-label>
          <input matInput type="date" [(ngModel)]="expiryDate" name="expiryDate" />
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid || !productId || submitting()" (click)="submit($event)">
        {{ submitting() ? 'Saving…' : 'Record restock' }}
      </button>
    </mat-dialog-actions>
  `,
})
export class RestockDialogComponent implements OnInit {
  private readonly dialogRef = inject(MatDialogRef<RestockDialogComponent>);
  private readonly productsService = inject(ProductService);
  private readonly stock = inject(StockService);
  private readonly toast = inject(ToastService);

  readonly productOptions = signal<Product[]>([]);
  readonly productTotal = signal(0);
  readonly hasMoreProducts = computed(() => this.productOptions().length < this.productTotal());
  readonly loadingProducts = signal(false);
  readonly loadingMoreProducts = signal(false);
  readonly productSearchText = signal('');
  readonly submitting = signal(false);

  productId: string | null = null;
  quantity: number | null = null;
  unitCost: number | null = null;
  supplierName = '';
  expiryDate: string | null = null;

  private productPage = 1;
  private readonly productPageSize = 8;
  private readonly productCache = new Map<string, Product>();
  private searchDebounce: ReturnType<typeof setTimeout> | null = null;

  private readonly onProductPanelScroll = (event: Event): void => {
    const panel = event.target as HTMLElement;
    if (panel.scrollHeight - panel.scrollTop - panel.clientHeight < 96) {
      void this.loadProductOptions(false);
    }
  };

  async ngOnInit(): Promise<void> {
    await this.loadProductOptions(true);
  }

  onProductInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.productSearchText.set(value);
    this.productId = null;
    if (this.searchDebounce) {
      clearTimeout(this.searchDebounce);
    }
    this.searchDebounce = setTimeout(() => {
      void this.loadProductOptions(true);
    }, 300);
  }

  onProductSelected(event: MatAutocompleteSelectedEvent): void {
    const id = event.option.value as string;
    this.productId = id;
    this.productSearchText.set(this.productCache.get(id)?.name ?? '');
  }

  onProductPanelOpened(): void {
    document.querySelector('.mat-mdc-autocomplete-panel')?.addEventListener('scroll', this.onProductPanelScroll);
  }

  onProductPanelClosed(): void {
    document.querySelector('.mat-mdc-autocomplete-panel')?.removeEventListener('scroll', this.onProductPanelScroll);
  }

  async loadProductOptions(reset: boolean): Promise<void> {
    if (reset) {
      this.productPage = 1;
    }
    if (this.loadingProducts() || this.loadingMoreProducts()) {
      return;
    }
    if (!reset && !this.hasMoreProducts()) {
      return;
    }
    if (reset) {
      this.loadingProducts.set(true);
    } else {
      this.loadingMoreProducts.set(true);
    }
    try {
      const result = await this.productsService.getProducts({
        search: this.productSearchText().trim(),
        page: this.productPage,
        pageSize: this.productPageSize,
      });
      for (const item of result.items) {
        this.productCache.set(item.id, item);
      }
      this.productOptions.set(reset ? result.items : [...this.productOptions(), ...result.items]);
      this.productTotal.set(result.totalCount);
      this.productPage += 1;
    } catch (error) {
      this.toast.error(firstDetail(error));
    } finally {
      this.loadingProducts.set(false);
      this.loadingMoreProducts.set(false);
    }
  }

  async submit(event: Event): Promise<void> {
    event.preventDefault();
    this.submitting.set(true);
    try {
      const body: RecordRestockRequest = {
        productId: this.productId!,
        quantity: Number(this.quantity),
        unitCost: Number(this.unitCost),
        supplierName: this.supplierName || null,
        expiryDate: this.expiryDate ? new Date(this.expiryDate).toISOString() : null,
      };
      await this.stock.recordRestock(body);
      this.toast.success('Restock recorded.');
      this.dialogRef.close(true);
    } catch (error) {
      this.toast.error(firstDetail(error));
    } finally {
      this.submitting.set(false);
    }
  }
}

@Component({
  selector: 'app-operations',
  imports: [
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatFormFieldModule,
MatPaginatorModule,
    MatProgressSpinnerModule,
    MatDialogModule,
    PageHeaderComponent,
    EmptyStateComponent,
  ],
  providers: [provideNativeDateAdapter()],
  templateUrl: './operations.page.html',
  styleUrl: './operations.page.scss',
})
export class OperationsPage implements OnInit {
  private readonly stock = inject(StockService);
  private readonly dialog = inject(MatDialog);
  private readonly toast = inject(ToastService);

  readonly movementTypes = MOVEMENT_TYPES;

  readonly loading = signal(false);
  readonly error = signal('');
  readonly movements = signal<StockMovement[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly type = signal('');
  readonly from = signal('');
  readonly to = signal('');

  readonly formatCurrency = formatCurrency;
  readonly formatDateTime = formatDateTime;

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
      const result = await this.stock.getMovements({
        movementType: this.type(),
        from: this.from(),
        to: this.to(),
        page: this.page(),
        pageSize: this.pageSize(),
      });
      this.movements.set(result.items);
      this.total.set(result.totalCount);
    } catch (error) {
      this.error.set(firstDetail(error));
    } finally {
      this.loading.set(false);
    }
  }

  openSale(): void {
    const ref = this.dialog.open(SaleDialogComponent, { width: '480px' });
    ref.afterClosed().subscribe(async (saved) => {
      if (saved) {
        await this.load();
      }
    });
  }

  openRestock(): void {
    const ref = this.dialog.open(RestockDialogComponent, { width: '480px' });
    ref.afterClosed().subscribe(async (saved) => {
      if (saved) {
        await this.load();
      }
    });
  }
}