import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatCardModule } from '@angular/material/card';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { NgxChartsModule } from '@swimlane/ngx-charts';
import { CategoryTotal, MonthTotal, StatusTotal } from '../../core/models/report';
import { ReportService } from '../../core/services/report.service';
import { CurrencyService } from '../../core/services/currency.service';
import { EmptyState } from '../../shared/empty-state/empty-state';

@Component({
  selector: 'app-reports',
  imports: [MatCardModule, MatProgressBarModule, NgxChartsModule, EmptyState],
  templateUrl: './reports.html',
  styleUrl: './reports.scss',
})
export class Reports {
  private readonly reportService = inject(ReportService);
  private readonly currencyService = inject(CurrencyService);

  readonly loading = signal(false);
  readonly categoryData = signal<{ name: string; value: number }[]>([]);
  readonly expenseMonthData = signal<{ name: string; value: number }[]>([]);
  readonly invoiceMonthData = signal<{ name: string; value: number }[]>([]);
  readonly statusData = signal<{ name: string; value: number }[]>([]);

  readonly colorScheme = 'cool';

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);

    this.reportService
      .getExpensesByCategory()
      .pipe(takeUntilDestroyed())
      .subscribe((data) => {
        this.categoryData.set(
          data.map((d: CategoryTotal) => ({ name: d.category, value: d.amount }))
        );
      });

    this.reportService
      .getExpensesByMonth(12)
      .pipe(takeUntilDestroyed())
      .subscribe((data) => {
        this.expenseMonthData.set(
          data.map((d: MonthTotal) => ({ name: this.monthLabel(d.month), value: d.amount }))
        );
      });

    this.reportService
      .getInvoicesByMonth(12)
      .pipe(takeUntilDestroyed())
      .subscribe((data) => {
        this.invoiceMonthData.set(
          data.map((d: MonthTotal) => ({ name: this.monthLabel(d.month), value: d.amount }))
        );
      });

    this.reportService
      .getInvoicesByStatus()
      .pipe(takeUntilDestroyed())
      .subscribe((data) => {
        this.statusData.set(
          data.map((d: StatusTotal) => ({ name: d.status, value: d.amount }))
        );
      });

    this.currencyService
      .getCurrencies()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: () => this.loading.set(false),
        error: () => this.loading.set(false),
      });
  }

  get currency(): string {
    return this.currencyService.defaultCurrency || 'USD';
  }

  monthLabel(month: string): string {
    const [year, m] = month.split('-').map(Number);
    return new Date(year, m - 1, 1).toLocaleDateString(undefined, { month: 'short' });
  }

  compactNumber = (value: number): string => {
    return new Intl.NumberFormat(undefined, { notation: 'compact' }).format(value);
  };

  currencyTick = (value: number): string => {
    return `${this.currency} ${this.compactNumber(value)}`;
  };
}
