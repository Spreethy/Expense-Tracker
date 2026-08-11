import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { Customer } from '../../core/models/customer';
import { CustomerService } from '../../core/services/customer.service';
import { EmptyState } from '../../shared/empty-state/empty-state';
import { ConfirmService } from '../../shared/services/confirm.service';
import { CustomerDialog } from './customer-dialog/customer-dialog';

@Component({
  selector: 'app-customers',
  imports: [
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressBarModule,
    MatTableModule,
    EmptyState,
  ],
  templateUrl: './customers.html',
  styleUrl: './customers.scss',
})
export class Customers {
  private readonly customerService = inject(CustomerService);
  private readonly dialog = inject(MatDialog);
  private readonly confirm = inject(ConfirmService);
  private readonly snackbar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  readonly columns = ['name', 'email', 'phone', 'actions'];
  readonly customers = signal<Customer[]>([]);
  readonly loading = signal(false);

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.customerService
      .getAll()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (customers) => this.customers.set(customers),
        error: () => this.snackbar.open('Could not load customers.', 'Close', { duration: 4000 }),
        complete: () => this.loading.set(false),
      });
  }

  openDialog(customer?: Customer): void {
    const ref = this.dialog.open(CustomerDialog, {
      data: customer,
      width: '480px',
    });

    ref
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((payload) => {
        if (!payload) return;
        const request = customer
          ? this.customerService.update(customer.id, payload)
          : this.customerService.create(payload);
        request.subscribe({
          next: () => {
            this.snackbar.open(customer ? 'Customer updated' : 'Customer created', 'Close', { duration: 3000 });
            this.load();
          },
          error: (err) => {
            this.snackbar.open(err.error?.message ?? 'Could not save the customer.', 'Close', { duration: 4000 });
          },
        });
      });
  }

  remove(customer: Customer): void {
    this.confirm
      .confirm({
        title: 'Delete customer',
        message: `Delete "${customer.name}"? This cannot be undone.`,
        confirmLabel: 'Delete',
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((confirmed) => {
        if (!confirmed) return;
        this.customerService.delete(customer.id).subscribe({
          next: () => {
            this.snackbar.open('Customer deleted', 'Close', { duration: 3000 });
            this.load();
          },
          error: () => {
            this.snackbar.open('Could not delete this customer.', 'Close', { duration: 4000 });
          },
        });
      });
  }
}
