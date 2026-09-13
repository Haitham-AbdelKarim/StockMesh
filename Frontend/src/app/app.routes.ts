import { Routes } from '@angular/router';
import { authGuard, ownerGuard } from './core/guards/app.guards';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/routes').then((m) => m.authRoutes),
  },
  {
    path: '',
    loadComponent: () => import('./shell/layout').then((m) => m.ShellLayout),
    canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        title: 'StockMesh · Dashboard',
        loadComponent: () => import('./features/dashboard/dashboard.page').then((m) => m.DashboardPage),
      },
      {
        path: 'inventory',
        title: 'StockMesh · Inventory',
        loadComponent: () => import('./features/inventory/inventory.page').then((m) => m.InventoryPage),
      },
      {
        path: 'operations',
        title: 'StockMesh · Operations',
        loadComponent: () => import('./features/operations/operations.page').then((m) => m.OperationsPage),
      },
      {
        path: 'network',
        title: 'StockMesh · Network',
        loadComponent: () => import('./features/network/network.page').then((m) => m.NetworkPage),
      },
      {
        path: 'reservations',
        title: 'StockMesh · Reservations',
        loadComponent: () => import('./features/reservations/reservations.page').then((m) => m.ReservationsPage),
      },
      {
        path: 'expenses',
        title: 'StockMesh · Expenses',
        loadComponent: () => import('./features/expenses/expenses.page').then((m) => m.ExpensesPage),
      },
      {
        path: 'recommendations',
        title: 'StockMesh · Recommendations',
        loadComponent: () =>
          import('./features/recommendations/recommendations.page').then((m) => m.RecommendationsPage),
      },
      {
        path: 'assistant',
        title: 'StockMesh · Assistant',
        loadComponent: () => import('./features/assistant/assistant.page').then((m) => m.AssistantPage),
      },
      {
        path: 'settings',
        title: 'StockMesh · Settings',
        canActivate: [ownerGuard],
        loadComponent: () => import('./features/settings/settings.page').then((m) => m.SettingsPage),
      },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];