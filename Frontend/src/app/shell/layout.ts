import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../core/services/auth.service';

interface NavItem {
  label: string;
  route: string;
  icon: string;
  ownerOnly: boolean;
}

const NAV_ITEMS: NavItem[] = [
  { label: 'Dashboard', route: '/dashboard', icon: 'dashboard', ownerOnly: false },
  { label: 'Inventory', route: '/inventory', icon: 'warehouse', ownerOnly: false },
  { label: 'Operations', route: '/operations', icon: 'sync_alt', ownerOnly: false },
  { label: 'Network', route: '/network', icon: 'hub', ownerOnly: false },
  { label: 'Reservations', route: '/reservations', icon: 'swap_horiz', ownerOnly: false },
  { label: 'Expenses', route: '/expenses', icon: 'receipt_long', ownerOnly: false },
  { label: 'Recommendations', route: '/recommendations', icon: 'auto_awesome', ownerOnly: false },
  { label: 'Assistant', route: '/assistant', icon: 'forum', ownerOnly: false },
  { label: 'Settings', route: '/settings', icon: 'settings', ownerOnly: true },
];

@Component({
  selector: 'app-shell',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatSidenavModule,
    MatToolbarModule,
    MatListModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatTooltipModule,
  ],
  templateUrl: './layout.html',
  styleUrl: './layout.scss',
})
export class ShellLayout {
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);

  readonly isOwner = this.auth.isOwner;
  readonly email = this.auth.email;
  readonly role = this.auth.role;
  readonly verticalCategory = this.auth.verticalCategory;
  readonly storeName = computed(() => this.auth.storeProfile()?.name ?? 'My Store');
  readonly initials = computed(() =>
    (this.auth.storeProfile()?.name ?? 'S').slice(0, 2).toUpperCase(),
  );

  readonly navItems = computed(() => NAV_ITEMS.filter((item) => !item.ownerOnly || this.isOwner()));

  async onLogout(): Promise<void> {
    try {
      await this.auth.logout();
    } finally {
      this.auth.clearSession();
      this.router.navigate(['/auth/login']);
    }
  }
}