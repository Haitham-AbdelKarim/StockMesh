import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import {
  MatDialog,
  MatDialogModule,
  MatDialogRef,
} from '@angular/material/dialog';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state';
import { Expense, RecordExpenseRequest } from '../../core/models/expense';
import { ExpenseService } from '../../core/services/expense.service';
import { ToastService } from '../../core/services/toast.service';
import { firstDetail } from '../../core/interceptors/api-error';
import { formatCurrency, formatDate } from '../../core/utils/format';

const COMMON_CATEGORIES = ['Rent', 'Utilities', 'Salaries', 'Supplies', 'Transport', 'Marketing', 'Other'];

@Component({
  selector: 'app-expense-dialog',
  imports: [FormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  template: `
    <h2 mat-dialog-title>Record an expense</h2>
    <mat-dialog-content class="dialog-content">
      <form #form="ngForm">
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Category</mat-label>
          <input matInput list="expense-categories" [(ngModel)]="category" name="category" required />
          <datalist id="expense-categories">
            @for (category of commonCategories; track category) {
              <option [value]="category"></option>
            }
          </datalist>
        </mat-form-field>

        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Amount</mat-label>
          <input matInput type="number" min="0.01" step="0.01" [(ngModel)]="amount" name="amount" required />
        </mat-form-field>

        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Date</mat-label>
          <input matInput type="date" [(ngModel)]="incurredAt" name="incurredAt" required />
        </mat-form-field>

        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Note (optional)</mat-label>
          <textarea matInput rows="2" [(ngModel)]="note" name="note"></textarea>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid || submitting()" (click)="submit($event)">
        {{ submitting() ? 'Saving…' : 'Record expense' }}
      </button>
    </mat-dialog-actions>
  `,
})
export class ExpenseDialogComponent {
  private readonly dialogRef = inject(MatDialogRef<ExpenseDialogComponent>);
  private readonly expenses = inject(ExpenseService);
  private readonly toast = inject(ToastService);

  readonly commonCategories = COMMON_CATEGORIES;
  readonly submitting = signal(false);

  category = '';
  amount: number | null = null;
  incurredAt = new Date();
  note = '';

  async submit(event: Event): Promise<void> {
    event.preventDefault();
    this.submitting.set(true);

    try {
      const body: RecordExpenseRequest = {
        category: this.category,
        amount: Number(this.amount),
        incurredAt: this.incurredAt.toISOString(),
        note: this.note || null,
      };
      await this.expenses.record(body);
      this.toast.success('Expense recorded.');
      this.dialogRef.close(true);
    } catch (error) {
      this.toast.error(firstDetail(error));
    } finally {
      this.submitting.set(false);
    }
  }
}

@Component({
  selector: 'app-expenses',
  imports: [
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    PageHeaderComponent,
    EmptyStateComponent,
  ],
  templateUrl: './expenses.page.html',
  styleUrl: './expenses.page.scss',
})
export class ExpensesPage implements OnInit {
  private readonly expenses = inject(ExpenseService);
  private readonly dialog = inject(MatDialog);

  readonly loading = signal(false);
  readonly error = signal('');
  readonly items = signal<Expense[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(20);

  readonly category = signal('');
  readonly from = signal('');
  readonly to = signal('');

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
      const result = await this.expenses.list({
        category: this.category(),
        from: this.from(),
        to: this.to(),
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

  openRecord(): void {
    const ref = this.dialog.open(ExpenseDialogComponent, { width: '460px' });
    ref.afterClosed().subscribe(async (saved) => {
      if (saved) {
        await this.load();
      }
    });
  }
}