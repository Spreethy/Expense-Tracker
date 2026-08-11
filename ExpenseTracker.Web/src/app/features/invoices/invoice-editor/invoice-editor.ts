import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AsyncPipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { map } from 'rxjs';
import { Customer } from '../../../core/models/customer';
import { InvoiceDetail } from '../../../core/models/invoice';
import { CustomerService } from '../../../core/services/customer.service';
import { CurrencyService } from '../../../core/services/currency.service';
import { InvoiceService } from '../../../core/services/invoice.service';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';

@Component({
  selector: 'app-invoice-editor',
  imports: [
    ReactiveFormsModule,
    AsyncPipe,
    MatButtonModule,
    MatCardModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatSelectModule,
    MoneyPipe,
  ],
  templateUrl: './invoice-editor.html',
  styleUrl: './invoice-editor.scss',
})
export class InvoiceEditor {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snackbar = inject(MatSnackBar);
  private readonly invoiceService = inject(InvoiceService);
  private readonly customerService = inject(CustomerService);
  private readonly currencyService = inject(CurrencyService);
  private readonly destroyRef = inject(DestroyRef);

  readonly customers = signal<Customer[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly editId = signal<number | null>(null);

  protected readonly currencies = this.currencyService
    .getCurrencies()
    .pipe(map((c) => c.currencies));

  protected readonly form = this.fb.nonNullable.group({
    customerId: [<number | null>null],
    issueDate: [new Date(), Validators.required],
    dueDate: [this.addDays(new Date(), 30), Validators.required],
    status: ['Draft'],
    taxRate: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
    currencyCode: [this.currencyService.defaultCurrency ?? 'USD', Validators.required],
    notes: [''],
    items: this.fb.array([this.newItem()], Validators.required),
  });

  constructor() {
    this.customerService
      .getAll()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((customers) => this.customers.set(customers));

    this.route.params.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      if (params['id']) {
        this.editId.set(Number(params['id']));
        this.loadForEdit(Number(params['id']));
      }
    });
  }

  private loadForEdit(id: number): void {
    this.loading.set(true);
    this.invoiceService.getById(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (invoice) => this.patchForm(invoice),
      error: () => this.router.navigate(['/invoices']),
      complete: () => this.loading.set(false),
    });
  }

  private patchForm(invoice: InvoiceDetail): void {
    this.form.patchValue({
      customerId: invoice.customerId,
      issueDate: new Date(invoice.issueDate),
      dueDate: new Date(invoice.dueDate),
      status: invoice.status === 'Draft' ? 'Draft' : 'Sent',
      taxRate: invoice.taxRate,
      currencyCode: invoice.currencyCode,
      notes: invoice.notes ?? '',
    });

    const items = this.form.controls.items;
    items.clear();
    invoice.items.forEach((item) => items.push(this.newItem(item.description, item.quantity, item.unitPrice)));
    if (items.length === 0) {
      items.push(this.newItem());
    }
  }

  get items(): FormArray {
    return this.form.controls.items;
  }

  private itemControls(index: number) {
    return this.items.at(index) as ReturnType<typeof this.newItem>;
  }

  private allItemControls(): ReturnType<typeof this.newItem>[] {
    return this.items.controls as ReturnType<typeof this.newItem>[];
  }

  protected newItem(description = '', quantity = 1, unitPrice = 0) {
    return this.fb.nonNullable.group({
      description: [description, [Validators.required, Validators.maxLength(200)]],
      quantity: [quantity, [Validators.required, Validators.min(0.01)]],
      unitPrice: [unitPrice, [Validators.required, Validators.min(0)]],
    });
  }

  protected addItem(): void {
    this.items.push(this.newItem());
  }

  protected removeItem(index: number): void {
    this.items.removeAt(index);
    if (this.items.length === 0) {
      this.items.push(this.newItem());
    }
  }

  protected itemTotal(index: number): number {
    const group = this.itemControls(index);
    const qty = group.controls.quantity.value;
    const price = group.controls.unitPrice.value;
    return (qty || 0) * (price || 0);
  }

  protected get subtotal(): number {
    return this.allItemControls().reduce(
      (sum, group) => sum + (group.controls.quantity.value || 0) * (group.controls.unitPrice.value || 0),
      0
    );
  }

  protected get tax(): number {
    return (this.subtotal * (this.form.controls.taxRate.value || 0)) / 100;
  }

  protected get total(): number {
    return this.subtotal + this.tax;
  }

  protected save(): void {
    if (this.form.invalid) {
      this.snackbar.open('Please fill in all required fields.', 'Close', { duration: 4000 });
      return;
    }

    const value = this.form.getRawValue();
    const payload = {
      customerId: value.customerId,
      issueDate: this.toApiDate(value.issueDate),
      dueDate: this.toApiDate(value.dueDate),
      status: (value.status === 'Sent' ? 'Sent' : 'Draft') as 'Draft' | 'Sent',
      taxRate: value.taxRate,
      currencyCode: value.currencyCode,
      notes: value.notes || null,
      items: value.items.map((item) => ({
        description: item.description,
        quantity: item.quantity,
        unitPrice: item.unitPrice,
      })),
    };

    this.saving.set(true);
    const request = this.editId()
      ? this.invoiceService.update(this.editId()!, payload)
      : this.invoiceService.create(payload);

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (invoice) => {
        this.saving.set(false);
        this.snackbar.open(`Invoice ${invoice.invoiceNumber} saved`, 'Close', { duration: 3000 });
        this.router.navigate(['/invoices', invoice.id]);
      },
      error: (err) => {
        this.saving.set(false);
        this.snackbar.open(err.error?.message ?? 'Could not save the invoice.', 'Close', { duration: 4000 });
      },
    });
  }

  protected cancel(): void {
    this.router.navigate(['/invoices']);
  }

  private addDays(date: Date, days: number): Date {
    const d = new Date(date);
    d.setDate(d.getDate() + days);
    return d;
  }

  private toApiDate(date: Date): string {
    return date.toISOString();
  }
}
