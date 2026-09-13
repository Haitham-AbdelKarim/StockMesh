/** @type {import('tailwindcss').Config} */
const materialColors = {
  primary: 'var(--mat-sys-primary)',
  'on-primary': 'var(--mat-sys-on-primary)',
  'primary-container': 'var(--mat-sys-primary-container)',
  'on-primary-container': 'var(--mat-sys-on-primary-container)',
  secondary: 'var(--mat-sys-secondary)',
  'on-secondary': 'var(--mat-sys-on-secondary)',
  'secondary-container': 'var(--mat-sys-secondary-container)',
  'on-secondary-container': 'var(--mat-sys-on-secondary-container)',
  tertiary: 'var(--mat-sys-tertiary)',
  'on-tertiary': 'var(--mat-sys-on-tertiary)',
  'tertiary-container': 'var(--mat-sys-tertiary-container)',
  'on-tertiary-container': 'var(--mat-sys-on-tertiary-container)',
  error: 'var(--mat-sys-error)',
  'on-error': 'var(--mat-sys-on-error)',
  'error-container': 'var(--mat-sys-error-container)',
  'on-error-container': 'var(--mat-sys-on-error-container)',
  surface: 'var(--mat-sys-surface)',
  'surface-dim': 'var(--mat-sys-surface-dim)',
  'surface-bright': 'var(--mat-sys-surface-bright)',
  'surface-container-low': 'var(--mat-sys-surface-container-low)',
  'surface-container': 'var(--mat-sys-surface-container)',
  'surface-container-high': 'var(--mat-sys-surface-container-high)',
  'surface-container-highest': 'var(--mat-sys-surface-container-highest)',
  'on-surface': 'var(--mat-sys-on-surface)',
  'on-surface-variant': 'var(--mat-sys-on-surface-variant)',
  outline: 'var(--mat-sys-outline)',
  'outline-variant': 'var(--mat-sys-outline-variant)',
};

module.exports = {
  content: ['./src/**/*.{html,ts,scss}'],
  theme: {
    extend: {
      colors: materialColors,
      fontFamily: {
        sans: ['Roboto', 'system-ui', 'sans-serif'],
      },
    },
  },
  plugins: [],
};