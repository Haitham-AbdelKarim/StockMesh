import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { lastValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Paginated } from '../models/paginated';
import { Expense, RecordExpenseRequest } from '../models/expense';
import { buildParams } from './catalog.service';

@Injectable({ providedIn: 'root' })
export class ExpenseService {
  private readonly http = inject(HttpClient);

  private get base(): string {
    return `${environment.apiBase}/api/v1/expenses`;
  }

  list(query: {
    category?: string;
    from?: string;
    to?: string;
    page?: number;
    pageSize?: number;
  }): Promise<Paginated<Expense>> {
    return lastValueFrom(
      this.http.get<Paginated<Expense>>(`${this.base}`, {
        params: buildParams({
          category: query.category ?? '',
          from: query.from ?? '',
          to: query.to ?? '',
          page: query.page ?? 1,
          pageSize: query.pageSize ?? 20,
        }),
      }),
    );
  }

  record(body: RecordExpenseRequest): Promise<Expense> {
    return lastValueFrom(this.http.post<Expense>(`${this.base}`, body));
  }
}