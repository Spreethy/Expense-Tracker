import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { InvoiceDetail as InvoiceDetailModel } from '../../../core/models/invoice';
import { InvoiceService } from '../../../core/services/invoice.service';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { StatusChip } from '../../../shared/status-chip/status-chip';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { ConfirmService } from '../../../shared/services/confirm.service';
import { PaymentDialog } from '../payment-dialog/payment-dialog';

@Component({
  selector: 'app-invoice-detail',
  imports: [
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressBarModule,
    MatTableModule,
    MoneyPipe,
    StatusChip,
    EmptyState,
  ],
  templateUrl: './invoice-detail.html',
  styleUrl: './invoice-detail.scss',
})
export class InvoiceDetail {
  private readonly route = inject(ActivatedRoute);
  protected readonly router = inject(Router);
  private readonly snackbar = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);
private readonly confirm = inject(ConfirmService);
  private readonly invoiceService = inject(InvoiceService);
  private readonly destroyRef = inject(DestroyRef);

  readonly invoice = signal<InvoiceDetailModel | null>(null);
  readonly loading = signal(false);
  readonly itemsColumns = ['description', 'quantity', 'unitPrice', 'amount'];
  readonly paymentsColumns = ['date', 'method', 'reference', 'amount', 'actions'];

  constructor() {
    this.route.params.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      this.load(Number(params['id']));
    });
  }

  load(id: number): void {
    this.loading.set(true);
    this.invoiceService
      .getById(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (invoice) => {
          this.invoice.set(invoice);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.snackbar.open('Invoice not found.', 'Close', { duration: 3000 });
          this.router.navigate(['/invoices']);
        },
      });
  }

  isEditable(): boolean {
    return this.invoice()?.status === 'Draft';
  }

  edit(): void {
    this.router.navigate(['/invoices', this.invoice()!.id, 'edit']);
  }

  send(): void {
    this.transition('Sent', 'Invoice marked as sent');
  }

  markPaid(): void {
    this.transition('Paid', 'Invoice marked as paid');
  }

  cancel(): void {
    this.transition('Cancelled', 'Invoice cancelled');
  }

  private transition(status: 'Sent' | 'Paid' | 'Cancelled', message: string): void {
    const invoice = this.invoice();
    if (!invoice) return;
    this.invoiceService.updateStatus(invoice.id, status).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (updated) => {
        this.invoice.set(updated);
        this.snackbar.open(message, 'Close', { duration: 3000 });
      },
      error: (err) => {
        this.snackbar.open(err.error?.message ?? 'Could not update status.', 'Close', { duration: 4000 });
      },
    });
  }

  addPayment(): void {
    const invoice = this.invoice();
    if (!invoice) return;

    const ref = this.dialog.open(PaymentDialog, {
      data: { balance: invoice.balance, currencyCode: invoice.currencyCode },
      width: '420px',
    });

    ref.afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe((payload) => {
      if (!payload) return;
      this.invoiceService.addPayment(invoice.id, payload).subscribe({
        next: (updated) => {
          this.invoice.set(updated);
          this.snackbar.open('Payment recorded', 'Close', { duration: 3000 });
        },
        error: (err) => {
          this.snackbar.open(err.error?.message ?? 'Could not record payment.', 'Close', { duration: 4000 });
        },
      });
    });
  }

  removePayment(paymentId: number): void {
    const invoice = this.invoice();
    if (!invoice) return;

    this.confirm
      .confirm({
        title: 'Remove payment',
        message: 'Remove this payment from the invoice?',
        confirmLabel: 'Remove',
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((confirmed) => {
        if (!confirmed) return;
        this.invoiceService.removePayment(invoice.id, paymentId).subscribe({
          next: (updated) => {
            this.invoice.set(updated);
            this.snackbar.open('Payment removed', 'Close', { duration: 3000 });
          },
          error: (err) => {
            this.snackbar.open(err.error?.message ?? 'Could not remove payment.', 'Close', { duration: 4000 });
          },
        });
      });
  }

  downloadPdf(): void {
    const invoice = this.invoice();
    if (!invoice) return;

    this.invoiceService.getPdf(invoice.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `${invoice.invoiceNumber}.pdf`;
        link.click();
        window.URL.revokeObjectURL(url);
      },
      error: () => {
        this.snackbar.open('Could not download the PDF.', 'Close', { duration: 4000 });
      },
    });
  }

  delete(): void {
    const invoice = this.invoice();
    if (!invoice) return;

    this.confirm
      .confirm({
        title: 'Delete invoice',
        message: `Delete ${invoice.invoiceNumber}? This cannot be undone.`,
        confirmLabel: 'Delete',
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((confirmed) => {
        if (!confirmed) return;
        this.invoiceService.delete(invoice.id).subscribe({
          next: () => this.router.navigate(['/invoices']),
          error: () => this.snackbar.open('Could not delete the invoice.', 'Close', { duration: 4000 }),
        });
      });
  }

  formatDate(value: string): string {
    return new Date(value).toLocaleDateString();
  }
}

