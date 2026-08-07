import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { Customer } from '../../core/models/customer';
import { Invoice } from '../../core/models/invoice';
import { InvoiceService } from '../../core/services/invoice.service';
import { CustomerService } from '../../core/services/customer.service';
import { MoneyPipe } from '../../shared/pipes/money.pipe';
import { EmptyState } from '../../shared/empty-state/empty-state';
import { StatusChip } from '../../shared/status-chip/status-chip';
import { ConfirmService } from '../../shared/services/confirm.service';

const STATUSES = ['Draft', 'Sent', 'Paid', 'Overdue', 'Cancelled'];

@Component({
  selector: 'app-invoices',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatProgressBarModule,
    MatSelectModule,
    MatTableModule,
    MoneyPipe,
    EmptyState,
    StatusChip,
  ],
  templateUrl: './invoices.html',
  styleUrl: './invoices.scss',
})
export class Invoices {
  private readonly invoiceService = inject(InvoiceService);
  private readonly customerService = inject(CustomerService);
  private readonly router = inject(Router);
  private readonly snackbar = inject(MatSnackBar);
  private readonly confirm = inject(ConfirmService);

  readonly columns = ['number', 'customer', 'issueDate', 'dueDate', 'status', 'total', 'balance', 'actions'];

  readonly invoices = signal<Invoice[]>([]);
  readonly customers = signal<Customer[]>([]);
  readonly loading = signal(false);

  readonly statusControl = new FormControl<string | null>(null);
  readonly customerControl = new FormControl<number | null>(null);

  constructor() {
    this.customerService
      .getAll()
      .pipe(takeUntilDestroyed())
      .subscribe((customers) => this.customers.set(customers));

    this.statusControl.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.load());
    this.customerControl.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.load());

    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.invoiceService
      .getAll({
        status: this.statusControl.value ?? undefined,
        customerId: this.customerControl.value ?? undefined,
      })
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: (invoices) => this.invoices.set(invoices),
        complete: () => this.loading.set(false),
      });
  }

  clearFilters(): void {
    this.statusControl.setValue(null);
    this.customerControl.setValue(null);
  }

  create(): void {
    this.router.navigate(['/invoices/new']);
  }

  open(invoice: Invoice): void {
    this.router.navigate(['/invoices', invoice.id]);
  }

  remove(invoice: Invoice): void {
    this.confirm
      .confirm({
        title: 'Delete invoice',
        message: `Delete ${invoice.invoiceNumber} (${invoice.total} ${invoice.currencyCode})? This cannot be undone.`,
        confirmLabel: 'Delete',
      })
      .pipe(takeUntilDestroyed())
      .subscribe((confirmed) => {
        if (!confirmed) return;
        this.invoiceService.delete(invoice.id).subscribe({
          next: () => {
            this.snackbar.open('Invoice deleted', 'Close', { duration: 3000 });
            this.load();
          },
          error: () => {
            this.snackbar.open('Could not delete this invoice.', 'Close', { duration: 4000 });
          },
        });
      });
  }

  formatDate(value: string): string {
    return new Date(value).toLocaleDateString();
  }

  protected readonly statuses = STATUSES;
}
