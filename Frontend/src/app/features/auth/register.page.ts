import { Component, AfterViewInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import L from 'leaflet';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { firstDetail } from '../../core/interceptors/api-error';
import { ProductService } from '../../core/services/catalog.service';

interface MapLocation {
  lat: number;
  lng: number;
}

const DEFAULT_CENTER: MapLocation = { lat: 31.5, lng: 34.75 };

@Component({
  selector: 'app-register',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './register.page.html',
  styleUrl: './auth-shared.scss',
})
export class RegisterPage implements AfterViewInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly productService = inject(ProductService);

  readonly verticalCategories = this.productService.verticalCategories();
  readonly submitting = signal(false);
  readonly serverError = signal('');
  readonly location = signal<MapLocation | null>(null);

  private map?: L.Map;
  private marker?: L.Marker;

  readonly form = new FormGroup({
    storeName: new FormControl('', [Validators.required, Validators.minLength(2), Validators.maxLength(150)]),
    verticalCategory: new FormControl('', [Validators.required]),
    email: new FormControl('', [Validators.required, Validators.email]),
    password: new FormControl('', [Validators.required, Validators.minLength(8)]),
    maxSearchRadiusKm: new FormControl<string | null>('', [Validators.required, Validators.min(1), Validators.max(500)]),
  });

  ngAfterViewInit(): void {
    this.initMap();
  }

  private initMap(): void {
    this.map = L.map('store-map', {
      center: [DEFAULT_CENTER.lat, DEFAULT_CENTER.lng],
      zoom: 6,
      scrollWheelZoom: true,
    });

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; OpenStreetMap contributors',
      maxZoom: 19,
    }).addTo(this.map);

    this.map.on('click', (event: L.LeafletMouseEvent) => {
      this.selectLocation(event.latlng.lat, event.latlng.lng);
    });
  }

  onMapReady(): void {
    this.map?.invalidateSize();
  }

  private selectLocation(lat: number, lng: number): void {
    this.location.set({ lat, lng });

    const pin = L.divIcon({
      className: '',
      html: '<div class="map-pin"></div>',
      iconSize: [28, 36],
      iconAnchor: [14, 34],
    });

    if (this.marker) {
      this.marker.setLatLng([lat, lng]);
      this.marker.setIcon(pin);
    } else if (this.map) {
      this.marker = L.marker([lat, lng], { icon: pin }).addTo(this.map);
    }
  }

  useMyLocation(): void {
    if (!navigator.geolocation) {
      this.toast.error('Geolocation is not supported by this browser.');
      return;
    }

    navigator.geolocation.getCurrentPosition(
      (position) => {
        this.selectLocation(position.coords.latitude, position.coords.longitude);
        this.map?.setView([position.coords.latitude, position.coords.longitude], 13);
        this.toast.success('Location set from your device.');
      },
      () => {
        this.toast.error('Could not get your location. Pick it on the map instead.');
      },
      { enableHighAccuracy: true, timeout: 8000, maximumAge: 60000 },
    );
  }

  clearLocation(): void {
    this.location.set(null);
    if (this.marker && this.map) {
      this.marker.remove();
      this.marker = undefined;
    }
  }

  async onSubmit(): Promise<void> {
    this.form.markAllAsTouched();
    const selected = this.location();
    if (this.form.invalid || this.submitting()) {
      return;
    }

    if (!selected) {
      this.toast.error('Please pick your store location on the map first.');
      return;
    }

    this.submitting.set(true);
    this.serverError.set('');

    try {
      const raw = this.form.getRawValue();
      const response = await this.auth.register({
        storeName: raw.storeName!,
        verticalCategory: raw.verticalCategory!,
        email: raw.email!,
        password: raw.password!,
        latitude: selected.lat,
        longitude: selected.lng,
        maxSearchRadiusKm: Number(raw.maxSearchRadiusKm),
      });
      this.auth.persistTokens(response.tokens);
      await this.auth.loadProfile();
      this.toast.success('Store created. Welcome to StockMesh!');
      this.router.navigate(['/dashboard']);
    } catch (error) {
      this.serverError.set(firstDetail(error));
    } finally {
      this.submitting.set(false);
    }
  }
}