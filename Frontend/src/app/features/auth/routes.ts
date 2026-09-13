import { Routes } from '@angular/router';
import { ownerGuard } from '../../core/guards/app.guards';

export const authRoutes: Routes = [
  { path: 'login', loadComponent: () => import('./login.page').then((m) => m.LoginPage) },
  { path: 'register', loadComponent: () => import('./register.page').then((m) => m.RegisterPage) },
  { path: 'join', canActivate: [ownerGuard], loadComponent: () => import('./join.page').then((m) => m.JoinPage) },
  { path: '', pathMatch: 'full', redirectTo: 'login' },
];