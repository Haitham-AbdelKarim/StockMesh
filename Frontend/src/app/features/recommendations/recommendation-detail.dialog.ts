import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { BaseChartDirective } from 'ng2-charts';
import { ChartData, ChartOptions } from 'chart.js';
import { MarketSignalPoint, Recommendation } from '../../core/models/recommendation';
import { RecommendationService } from '../../core/services/recommendation.service';
import { recommendationActionClass } from '../../core/utils/recommendations';

export interface RecommendationDetailData {
  recommendation: Recommendation;
}

@Component({
  selector: 'app-recommendation-detail-dialog',
  imports: [
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    BaseChartDirective,
    DatePipe,
  ],
  templateUrl: './recommendation-detail.dialog.html',
  styleUrl: './recommendation-detail.dialog.scss',
})
export class RecommendationDetailDialogComponent implements OnInit {
  private readonly recommendations = inject(RecommendationService);
  private readonly data = inject<RecommendationDetailData>(MAT_DIALOG_DATA);

  readonly item = signal(this.data.recommendation);
  readonly market = signal<MarketSignalPoint[] | null>(null);
  readonly marketError = signal('');

  readonly actionClass = recommendationActionClass;

  readonly forecastChart = computed<ChartData<'line'> | null>(() => {
    const forecast = this.item().forecast;
    if (!forecast || !forecast.dates.length) {
      return null;
    }

    return {
      labels: forecast.dates,
      datasets: [
        {
          label: 'Actual sales',
          data: [...forecast.actuals],
          borderColor: '#1b5e9e',
          tension: 0.2,
          pointRadius: 0,
        },
        {
          label: 'Forecast',
          data: [...forecast.yhat],
          borderColor: '#7b1fa2',
          borderDash: [6, 4],
          tension: 0.35,
          pointRadius: 0,
        },
        {
          label: 'Lower bound',
          data: [...forecast.lower],
          borderColor: 'rgba(123, 31, 162, 0.25)',
          tension: 0.35,
          pointRadius: 0,
        },
        {
          label: 'Upper bound',
          data: [...forecast.upper],
          borderColor: 'rgba(123, 31, 162, 0.25)',
          backgroundColor: 'rgba(123, 31, 162, 0.12)',
          fill: '-1',
          tension: 0.35,
          pointRadius: 0,
        },
      ],
    };
  });

  readonly chartOptions: ChartOptions<'line'> = {
    responsive: true,
    maintainAspectRatio: false,
    interaction: { mode: 'index', intersect: false },
    scales: {
      y: { beginAtZero: true },
      x: { ticks: { maxTicksLimit: 8, maxRotation: 45 } },
    },
  };

  readonly marketChart = computed<ChartData<'line'> | null>(() => {
    const points = this.market();
    if (!points || !points.length) {
      return null;
    }

    return {
      labels: points.map((point) =>
        new Date(point.date).toLocaleDateString('en-US', { month: 'short', day: 'numeric' }),
      ),
      datasets: [
        {
          label: 'Transfer volume',
          data: points.map((point) => point.transferVolume),
          borderColor: '#1b5e9e',
          backgroundColor: 'rgba(27, 94, 158, 0.12)',
          fill: true,
          tension: 0.35,
          pointRadius: 3,
        },
        {
          label: 'Reservations',
          data: points.map((point) => point.reservationCount),
          borderColor: '#7b1fa2',
          tension: 0.35,
          pointRadius: 3,
        },
      ],
    };
  });

  async ngOnInit(): Promise<void> {
    if (this.item().recommendedAction !== 'MarketOpportunity') {
      return;
    }

    try {
      this.market.set(await this.recommendations.marketHistory(this.item().productId));
    } catch {
      this.marketError.set('Could not load network demand history.');
    }
  }
}
