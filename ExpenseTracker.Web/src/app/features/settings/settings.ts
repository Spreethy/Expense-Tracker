import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { CurrencyInfo, UserCurrency } from '../../core/models/currency';
import { CurrencyService } from '../../core/services/currency.service';

@Component({
  selector: 'app-settings',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatSelectModule,
    MatTableModule,
  ],
  templateUrl: './settings.html',
  styleUrl: './settings.scss',
})
export class Settings {
  private readonly fb = inject(FormBuilder);
  private readonly currencyService = inject(CurrencyService);
  private readonly snackbar = inject(MatSnackBar);

  readonly loading = signal(false);
  readonly rates = signal<CurrencyInfo[]>([]);
  readonly currency = signal<{ code: string; name: string }[]>([]);
  readonly columns = ['currency', 'rate'];

  protected readonly defaultForm = this.fb.nonNullable.group({
    defaultCurrency: ['USD', Validators.required],
  });

  protected readonly ratesForm = this.fb.nonNullable.group<Record<string, FormControl<number | null>>>({});

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.currencyService
      .getCurrencies()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: (data) => this.apply(data),
        error: () => {
          this.loading.set(false);
          this.snackbar.open('Could not load settings.', 'Close', { duration: 4000 });
        },
      });
  }

  private apply(data: UserCurrency): void {
    const codes = data.currencies.map((c) => c.code).sort();
    this.currency.set(data.currencies.map((c) => ({ code: c.code, name: c.name })));
    this.defaultForm.patchValue({ defaultCurrency: data.defaultCurrency });

    const group: Record<string, FormControl<number | null>> = {};
    for (const code of codes) {
      if (code !== data.defaultCurrency) {
        const info = data.currencies.find((c) => c.code === code);
        group[code] = this.fb.control<number | null>(info?.rateToDefault ?? 1, [
          Validators.required,
          Validators.min(0.000001),
        ]);
      }
    }
    this.ratesForm.reset(group);
    this.rates.set(data.currencies.filter((c) => c.code !== data.defaultCurrency));
    this.loading.set(false);
  }

  saveDefault(): void {
    const code = this.defaultForm.controls.defaultCurrency.value;
    this.currencyService.updateDefault(code).pipe(takeUntilDestroyed()).subscribe({
      next: (data) => {
        this.apply(data);
        this.snackbar.open(`Default currency set to ${code}`, 'Close', { duration: 3000 });
      },
      error: (err) => {
        this.snackbar.open(err.error?.message ?? 'Could not update default currency.', 'Close', { duration: 4000 });
      },
    });
  }

  saveRates(): void {
    if (this.ratesForm.invalid) {
      this.snackbar.open('Please enter valid rates.', 'Close', { duration: 4000 });
      return;
    }
    const rates: Record<string, number> = {};
    for (const key of Object.keys(this.ratesForm.controls)) {
      rates[key] = this.ratesForm.controls[key].value ?? 1;
    }
    this.currencyService.updateRates(rates).pipe(takeUntilDestroyed()).subscribe({
      next: (data) => {
        this.apply(data);
        this.snackbar.open('Exchange rates saved', 'Close', { duration: 3000 });
      },
      error: (err) => {
        this.snackbar.open(err.error?.message ?? 'Could not save rates.', 'Close', { duration: 4000 });
      },
    });
  }

  rateControl(code: string): FormControl<number | null> {
    return this.ratesForm.controls[code];
  }
}
