import { RecommendedAction } from '../models/recommendation';

export function recommendationSeverity(action: RecommendedAction): number {
  switch (action) {
    case 'UrgentShare':
      return 4;
    case 'Reorder':
      return 3;
    case 'Share':
      return 2;
    case 'MarketOpportunity':
      return 1;
    default:
      return 0;
  }
}

export function recommendationActionClass(action: RecommendedAction): string {
  return `action-${action.toLowerCase()}`;
}

export function recommendationActionIcon(action: RecommendedAction): string {
  switch (action) {
    case 'UrgentShare':
      return 'warning';
    case 'Reorder':
      return 'shopping_cart';
    case 'Share':
      return 'share';
    case 'MarketOpportunity':
      return 'trending_up';
    default:
      return 'check_circle';
  }
}
