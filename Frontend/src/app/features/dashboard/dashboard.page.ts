import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BaseChartDirective } from 'ng2-charts';
import { ChartData, ChartOptions } from 'chart.js';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DatePipe } from '@angular/common';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { StatCardComponent } from '../../shared/stat-card/stat-card';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state';
import { DashboardService } from '../../core/services/dashboard.service';
import { RecommendationService } from '../../core/services/recommendation.service';
import {
  DashboardSummary,
  LowStockItem,
  NetworkSummary,
  SalesTrendPoint,
  SlowMover,
  TopSeller,
} from '../../core/models/dashboard';
import { Recommendation } from '../../core/models/recommendation';
import {
  recommendationActionClass,
  recommendationActionIcon,
  recommendationSeverity,
} from '../../core/utils/recommendations';
import { formatCurrency, formatCurrencyCompact } from '../../core/utils/format';
import { firstDetail } from '../../core/interceptors/api-error';

const shortDateFormatter = new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric' });

function toDateKey(value: Date): string {
  return value.toISOString().slice(0, 10);
}

function shiftDateKey(key: string, deltaDays: number): string {
  const date = new Date(`${key}T00:00:00Z`);
  date.setUTCDate(date.getUTCDate() + deltaDays);
  return toDateKey(date);
}

@Component({
  selector: 'app-dashboard',
  imports: [
    PageHeaderComponent,
    StatCardComponent,
    EmptyStateComponent,
    BaseChartDirective,
    MatButtonModule,
    MatCardModule,
    MatDividerModule,
    MatIconModule,
    MatButtonToggleModule,
    MatProgressSpinnerModule,
    RouterLink,
    DatePipe,
  ],
  templateUrl: './dashboard.page.html',
  styleUrl: './dashboard.page.scss',
})
export class DashboardPage implements OnInit {
  private readonly dashboard = inject(DashboardService);
  private readonly recommendations = inject(RecommendationService);

  readonly loading = signal(true);
  readonly error = signal('');
  readonly days = signal(14);
  readonly selectedDate = signal(toDateKey(new Date()));
  readonly summaryLoading = signal(false);

  readonly summary = signal<DashboardSummary | null>(null);
  readonly network = signal<NetworkSummary | null>(null);
  readonly topSellers = signal<TopSeller[]>([]);
  readonly slowMovers = signal<SlowMover[]>([]);
  readonly lowStock = signal<LowStockItem[]>([]);
  readonly trend = signal<SalesTrendPoint[]>([]);
  readonly attentionItems = signal<Recommendation[]>([]);

  readonly todayKey = toDateKey(new Date());
  readonly selectedDateObj = computed(() => new Date(`${this.selectedDate()}T00:00:00`));
  readonly isTodaySelected = computed(() => this.selectedDate() === this.todayKey);
  readonly hasActivity = computed(() => {
    const s = this.summary();
    return (
      !!s &&
      (s.salesRevenue !== 0 ||
        s.transfersOutRevenue !== 0 ||
        s.stockPurchases !== 0 ||
        s.expenseTotal !== 0 ||
        s.unitsSold !== 0 ||
        s.transfersOutUnits !== 0)
    );
  });

  readonly formatCurrency = formatCurrency;

  readonly unitsSold = computed(() => this.summary()?.unitsSold ?? 0);
  readonly totalRevenue = computed(
    () => (this.summary()?.salesRevenue ?? 0) + (this.summary()?.transfersOutRevenue ?? 0),
  );
  readonly totalSpending = computed(
    () => (this.summary()?.stockPurchases ?? 0) + (this.summary()?.expenseTotal ?? 0),
  );
  readonly spendingHint = computed(() => {
    const s = this.summary();
    return `Stock ${formatCurrency(s?.stockPurchases ?? 0)} · Expenses ${formatCurrency(s?.expenseTotal ?? 0)}`;
  });
  readonly grossMarginText = computed(() => {
    const s = this.summary();
    if (!s) {
      return '—';
    }
    const revenue = s.salesRevenue + s.transfersOutRevenue;
    if (revenue <= 0) {
      return '—';
    }
    const margin = ((revenue - s.costOfGoodsSold) / revenue) * 100;
    return `Gross margin ${margin.toFixed(1)}%`;
  });

  readonly trendLabels = computed(() => this.trend().map((point) => shortDateFormatter.format(new Date(point.date))));
  readonly trendRevenue = computed(() => this.trend().map((point) => point.salesRevenue));
  readonly trendUnits = computed(() => this.trend().map((point) => point.unitsSold));

  readonly sellerLabels = computed(() => this.topSellers().map((seller) => seller.productName));
  readonly sellerUnits = computed(() => this.topSellers().map((seller) => seller.unitsSold));
  readonly sellerRevenue = computed(() => this.topSellers().map((seller) => seller.salesRevenue));

  readonly successRate = computed(() => (this.network()?.reservationSuccessRate ?? 0) * 100);

  readonly actionClass = recommendationActionClass;
  readonly actionIcon = recommendationActionIcon;

  readonly lineChartData = computed<ChartData<'line'>>(() => ({
    labels: this.trendLabels(),
    datasets: [
      {
        label: 'Revenue',
        data: this.trendRevenue(),
        borderColor: '#1b5e9e',
        backgroundColor: 'rgba(27, 94, 158, 0.12)',
        fill: true,
        tension: 0.35,
        pointRadius: 3,
      },
      {
        label: 'Units',
        data: this.trendUnits(),
        borderColor: '#7b1fa2',
        backgroundColor: 'rgba(123, 31, 162, 0.10)',
        fill: true,
        tension: 0.35,
        pointRadius: 3,
        yAxisID: 'yUnits',
      },
    ],
  }));

  readonly lineChartOptions: ChartOptions<'line'> = {
    responsive: true,
    maintainAspectRatio: false,
    interaction: { mode: 'index', intersect: false },
    scales: {
      y: { beginAtZero: true, ticks: { callback: (value) => formatCurrencyCompact(Number(value)) } },
      yUnits: { beginAtZero: true, position: 'right', grid: { display: false } },
    },
  };

  readonly barChartData = computed<ChartData<'bar'>>(() => ({
    labels: this.sellerLabels(),
    datasets: [
      {
        label: 'Units sold',
        data: this.sellerUnits(),
        backgroundColor: 'rgba(27, 94, 158, 0.75)',
        borderRadius: 6,
        yAxisID: 'yUnits',
      },
      {
        label: 'Revenue',
        data: this.sellerRevenue(),
        backgroundColor: 'rgba(123, 31, 162, 0.55)',
        borderRadius: 6,
      },
    ],
  }));

  readonly barChartOptions: ChartOptions<'bar'> = {
    responsive: true,
    maintainAspectRatio: false,
    scales: {
      y: { beginAtZero: true, ticks: { callback: (value) => formatCurrencyCompact(Number(value)) } },
      yUnits: { beginAtZero: true, position: 'right', grid: { display: false } },
    },
  };

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  async onDaysChange(days: number): Promise<void> {
    this.days.set(days);
    await this.loadTrend();
  }

  async stepDay(delta: -1 | 1): Promise<void> {
    await this.onDateSelected(shiftDateKey(this.selectedDate(), delta));
  }

  async goToday(): Promise<void> {
    await this.onDateSelected(this.todayKey);
  }

  onDateInput(event: Event): void {
    const value = (event.target as HTMLInputElement | null)?.value ?? '';
    void this.onDateSelected(value);
  }

  async onDateSelected(key: string): Promise<void> {
    const normalized = !key || key > this.todayKey ? this.todayKey : key;
    if (normalized === this.selectedDate() && this.summary()) {
      return;
    }
    this.selectedDate.set(normalized);
    await this.loadSummary();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.error.set('');

    try {
      const summary = await this.dashboard.summary(this.selectedDate());
      this.summary.set(summary);

      await this.loadTrend();
      await this.loadPanels();
    } catch (error) {
      this.error.set(`Could not load dashboard. ${firstDetail(error)}`);
    } finally {
      this.loading.set(false);
    }
  }

  private async loadSummary(): Promise<void> {
    this.summaryLoading.set(true);
    try {
      this.summary.set(await this.dashboard.summary(this.selectedDate()));
    } catch (error) {
      this.error.set(`Could not load dashboard. ${firstDetail(error)}`);
    } finally {
      this.summaryLoading.set(false);
    }
  }

  private async loadTrend(): Promise<void> {
    const [trend, topSellers] = await Promise.all([
      this.dashboard.salesTrend(this.days()),
      this.dashboard.topSellers({ topN: 6 }),
    ]);
    this.trend.set(trend);
    this.topSellers.set(topSellers);
  }

  private async loadPanels(): Promise<void> {
    const [network, slowMovers, lowStock, attention] = await Promise.all([
      this.dashboard.network(30),
      this.dashboard.slowMovers(7),
      this.dashboard.lowStock(),
      this.recommendations.list({ page: 1, pageSize: 50 }).catch(() => null),
    ]);
    this.network.set(network);
    this.slowMovers.set(slowMovers);
    this.lowStock.set(lowStock);
    this.attentionItems.set(
      (attention?.items ?? [])
        .filter((item) => item.recommendedAction !== 'Hold')
        .sort((a, b) => recommendationSeverity(b.recommendedAction) - recommendationSeverity(a.recommendedAction))
        .slice(0, 5),
    );
  }
}