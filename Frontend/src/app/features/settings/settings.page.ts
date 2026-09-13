import { Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { AuthService } from '../../core/services/auth.service';
import { StoreService } from '../../core/services/catalog.service';
import { PaymentService } from '../../core/services/payment.service';
import { ToastService } from '../../core/services/toast.service';
import { firstDetail } from '../../core/interceptors/api-error';

@Component({
  selector: 'app-settings',
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatDividerModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    PageHeaderComponent,
  ],
  templateUrl: './settings.page.html',
  styleUrl: './settings.page.scss',
})
export class SettingsPage implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly stores = inject(StoreService);
  private readonly payments = inject(PaymentService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  readonly profile = this.auth.storeProfile;
  readonly role = this.auth.role;
  readonly email = this.auth.email;
  readonly storeId = this.auth.storeId;

  readonly loading = signal(true);
  readonly saving = signal(false);

  readonly connectLoading = signal(true);
  readonly connectAccountId = signal<string | null>(null);
  readonly payoutsEnabled = signal(false);
  readonly connecting = signal(false);

  readonly form = new FormGroup({
    name: new FormControl('', [Validators.required, Validators.minLength(2), Validators.maxLength(150)]),
    latitude: new FormControl<number>(0, [Validators.required]),
    longitude: new FormControl<number>(0, [Validators.required]),
    maxSearchRadiusKm: new FormControl<number>(10, [Validators.required, Validators.min(1), Validators.max(500)]),
  });

  async ngOnInit(): Promise<void> {
    if (!this.profile()) {
      const refreshed = await this.auth.loadProfile();
      if (refreshed) {
        this.patchForm(refreshed);
      }
    } else {
      this.patchForm(this.profile()!);
    }
    this.loading.set(false);
    await this.refreshConnectStatus();
  }

  async refreshConnectStatus(): Promise<void> {
    this.connectLoading.set(true);
    try {
      const status = await this.payments.connectStatus();
      this.connectAccountId.set(status.accountId);
      this.payoutsEnabled.set(status.payoutsEnabled);
    } catch {
      this.connectAccountId.set(null);
      this.payoutsEnabled.set(false);
    } finally {
      this.connectLoading.set(false);
    }
  }

  async onConnect(): Promise<void> {
    if (this.connecting()) {
      return;
    }
    this.connecting.set(true);
    try {
      const link = await this.payments.onboard();
      window.location.href = link.onboardingUrl;
    } catch (error) {
      this.toast.error(firstDetail(error));
      this.connecting.set(false);
    }
  }

  private patchForm(profile: { name: string; latitude: number; longitude: number; maxSearchRadiusKm: number }): void {
    this.form.patchValue({
      name: profile.name,
      latitude: profile.latitude,
      longitude: profile.longitude,
      maxSearchRadiusKm: profile.maxSearchRadiusKm,
    });
  }

  async onSave(): Promise<void> {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.saving()) {
      return;
    }

    this.saving.set(true);
    try {
      const raw = this.form.getRawValue();
      await this.stores.updateProfile({
        name: raw.name!,
        latitude: Number(raw.latitude),
        longitude: Number(raw.longitude),
        maxSearchRadiusKm: Number(raw.maxSearchRadiusKm),
      });
      await this.auth.loadProfile();
      this.toast.success('Store profile updated.');
    } catch (error) {
      this.toast.error(firstDetail(error));
    } finally {
      this.saving.set(false);
    }
  }

  goToJoin(): void {
    this.router.navigate(['/auth/join']);
  }
}