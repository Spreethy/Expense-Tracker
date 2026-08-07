import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { RouterLink } from '@angular/router';
import { NgxChartsModule } from '@swimlane/ngx-charts';
import { DashboardSummary, MonthTotal, StatusTotal } from '../../core/models/report';
import { ReportService } from '../../core/services/report.service';
import { MoneyPipe } from '../../shared/pipes/money.pipe';
import { EmptyState } from '../../shared/empty-state/empty-state';

interface CardDef {
  label: string;
  value: number;
  icon: string;
  link?: string;
  accent?: boolean;
  warn?: boolean;
}

@Component({
  selector: 'app-dashboard',
  imports: [
    MatCardModule,
    MatIconModule,
    MatProgressBarModule,
    RouterLink,
    NgxChartsModule,
    MoneyPipe,
    EmptyState,
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard {
  private readonly reportService = inject(ReportService);

  readonly loading = signal(false);
  readonly summary = signal<DashboardSummary | null>(null);
  readonly statusData = signal<{ name: string; value: number }[]>([]);
  readonly monthData = signal<{ name: string; value: number }[]>([]);

  readonly colorScheme = 'cool';

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.reportService
      .getSummary()
      .pipe(takeUntilDestroyed())
      .subscribe((summary) => {
        this.summary.set(summary);
        this.loading.set(false);
      });

    this.reportService
      .getInvoicesByStatus()
      .pipe(takeUntilDestroyed())
      .subscribe((data) => {
        this.statusData.set(
          data.map((d) => ({ name: d.status, value: d.amount }))
        );
      });

    this.reportService
      .getExpensesByMonth(6)
      .pipe(takeUntilDestroyed())
      .subscribe((data) => {
        this.monthData.set(
          data.map((d) => ({ name: this.monthLabel(d.month), value: d.amount }))
        );
      });
  }

  get cards(): CardDef[] {
    const s = this.summary();
    if (!s) return [];
    return [
      { label: 'Expenses this month', value: s.totalExpensesThisMonth, icon: 'receipt_long' },
      { label: 'Expenses all time', value: s.totalExpensesAllTime, icon: 'history' },
      { label: 'Invoiced', value: s.totalInvoiced, icon: 'description' },
      { label: 'Paid', value: s.totalPaid, icon: 'payments', accent: true },
      { label: 'Outstanding', value: s.totalOutstanding, icon: 'schedule', warn: true },
    ];
  }

  get counts(): CardDef[] {
    const s = this.summary();
    if (!s) return [];
    return [
      { label: 'Expenses', value: s.expenseCount, icon: 'receipt_long', link: '/expenses' },
      { label: 'Invoices', value: s.invoiceCount, icon: 'description', link: '/invoices' },
      { label: 'Overdue', value: s.overdueInvoiceCount, icon: 'warning', link: '/invoices', warn: true },
      { label: 'Customers', value: s.customerCount, icon: 'people', link: '/customers' },
    ];
  }

  get currency(): string {
    return this.summary()?.currency ?? '';
  }

  monthLabel(month: string): string {
    const [year, m] = month.split('-').map(Number);
    return new Date(year, m - 1, 1).toLocaleDateString(undefined, { month: 'short' });
  }

  compactNumber = (value: number): string => {
    return new Intl.NumberFormat(undefined, { notation: 'compact' }).format(value);
  };
}
