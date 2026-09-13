import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state';
import { Recommendation, RecommendedAction } from '../../core/models/recommendation';
import { RECOMMENDED_ACTIONS } from '../../core/models/enums';
import { recommendationActionClass } from '../../core/utils/recommendations';
import { RecommendationService } from '../../core/services/recommendation.service';
import { RecommendationDetailDialogComponent } from './recommendation-detail.dialog';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { firstDetail } from '../../core/interceptors/api-error';

@Component({
  selector: 'app-recommendations',
  imports: [
    MatCardModule,
    MatButtonModule,
    MatDialogModule,
    MatIconModule,
    MatSelectModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    PageHeaderComponent,
    EmptyStateComponent,
    DatePipe,
  ],
  templateUrl: './recommendations.page.html',
  styleUrl: './recommendations.page.scss',
})
export class RecommendationsPage implements OnInit {
  private readonly recommendations = inject(RecommendationService);
  private readonly dialog = inject(MatDialog);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);

  readonly actions = RECOMMENDED_ACTIONS;
  readonly isOwner = this.auth.isOwner;

  readonly loading = signal(false);
  readonly running = signal(false);
  readonly error = signal('');
  readonly items = signal<Recommendation[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(20);

  readonly action = signal<RecommendedAction | ''>('');

  readonly attentionCount = computed(() => this.items().filter((item) => item.recommendedAction !== 'Hold').length);

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  async onActionChange(): Promise<void> {
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
      const result = await this.recommendations.list({
        recommendedAction: this.action() || undefined,
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

  actionClass(action: RecommendedAction): string {
    return recommendationActionClass(action);
  }

  modelLabel(modelVersion: string): string {
    return modelVersion === 'prophet-v1' ? 'AI forecast' : 'Rule estimate';
  }

  async run(): Promise<void> {    this.running.set(true);

    try {
      const summary = await this.recommendations.run();
      this.toast.success(
        `Evaluation complete — ${summary.productsEvaluated} products, ${summary.recommendationsWritten} recommendations written.`,
      );
      await this.load();
    } catch (error) {
      this.toast.error(firstDetail(error));
    } finally {
      this.running.set(false);
    }
  }

  openDetail(item: Recommendation): void {
    this.dialog.open(RecommendationDetailDialogComponent, {
      width: '760px',
      maxWidth: '94vw',
      data: { recommendation: item },
    });
  }
}
