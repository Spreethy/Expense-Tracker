import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { PaymentMethod } from '../../../core/models/invoice';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';

export interface PaymentDialogData {
  balance: number;
  currencyCode: string;
}

const METHODS: PaymentMethod[] = ['Cash', 'Bank', 'Card', 'Other'];

@Component({
  selector: 'app-payment-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MoneyPipe,
  ],
  templateUrl: './payment-dialog.html',
  styleUrl: './payment-dialog.scss',
})
export class PaymentDialog {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<PaymentDialog>);
  private readonly data = inject<PaymentDialogData>(MAT_DIALOG_DATA);

  protected readonly balance = this.data.balance;
  protected readonly currencyCode = this.data.currencyCode;
  protected readonly methods = METHODS;

  protected readonly form = this.fb.nonNullable.group({
    amount: [null as number | null, [Validators.required, Validators.min(0.01), Validators.max(this.balance)]],
    paymentDate: [new Date(), Validators.required],
    method: ['Bank' as PaymentMethod],
    reference: ['', [Validators.maxLength(100)]],
  });

  protected save(): void {
    if (this.form.invalid) {
      return;
    }
    const value = this.form.getRawValue();
    this.dialogRef.close({
      amount: value.amount,
      paymentDate: value.paymentDate,
      method: value.method,
      reference: value.reference || null,
    });
  }
}
