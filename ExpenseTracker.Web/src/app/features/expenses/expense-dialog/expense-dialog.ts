import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AsyncPipe } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { map } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Category } from '../../../core/models/category';
import { CurrencyService } from '../../../core/services/currency.service';
import { Expense } from '../../../core/models/expense';

export interface ExpenseDialogData {
  expense?: Expense;
  categories: Category[];
}

@Component({
  selector: 'app-expense-dialog',
  imports: [
    ReactiveFormsModule,
    AsyncPipe,
    MatDialogModule,
    MatButtonModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
  ],
  templateUrl: './expense-dialog.html',
  styleUrl: './expense-dialog.scss',
})
export class ExpenseDialog {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<ExpenseDialog>);
  private readonly data = inject<ExpenseDialogData>(MAT_DIALOG_DATA);
  private readonly currencyService = inject(CurrencyService);

  protected readonly isEdit = !!this.data.expense;
  protected readonly categories = this.data.categories;
  protected readonly currencies = this.currencyService.getCurrencies().pipe(map((c) => c.currencies));

  protected readonly form = this.fb.nonNullable.group({
    description: [this.data.expense?.description ?? '', [Validators.required, Validators.maxLength(200)]],
    amount: [this.data.expense?.amount ?? null, [Validators.required, Validators.min(0.01)]],
    currencyCode: [
      this.data.expense?.currencyCode ?? this.currencyService.defaultCurrency ?? 'USD',
      [Validators.required, Validators.maxLength(3)],
    ],
    categoryId: [this.data.expense?.categoryId ?? null],
    expenseDate: [this.data.expense ? new Date(this.data.expense.expenseDate) : new Date(), [Validators.required]],
    notes: [this.data.expense?.notes ?? '', [Validators.maxLength(500)]],
  });

  protected save(): void {
    if (this.form.invalid) {
      return;
    }
    const value = this.form.getRawValue();
    this.dialogRef.close({
      description: value.description,
      amount: value.amount,
      currencyCode: value.currencyCode,
      categoryId: value.categoryId,
      expenseDate: value.expenseDate,
      notes: value.notes || null,
    });
  }
}
