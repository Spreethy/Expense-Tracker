import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { combineLatestWith } from 'rxjs';
import { Category } from '../../core/models/category';
import { CurrencyService } from '../../core/services/currency.service';
import { Expense } from '../../core/models/expense';
import { ExpenseService } from '../../core/services/expense.service';
import { CategoryService } from '../../core/services/category.service';
import { MoneyPipe } from '../../shared/pipes/money.pipe';
import { EmptyState } from '../../shared/empty-state/empty-state';
import { ConfirmService } from '../../shared/services/confirm.service';
import { ExpenseDialog } from './expense-dialog/expense-dialog';

@Component({
  selector: 'app-expenses',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatSelectModule,
    MatTableModule,
    MoneyPipe,
    EmptyState,
  ],
  templateUrl: './expenses.html',
  styleUrl: './expenses.scss',
})
export class Expenses {
  private readonly expenseService = inject(ExpenseService);
  private readonly categoryService = inject(CategoryService);
  private readonly currencyService = inject(CurrencyService);
  private readonly dialog = inject(MatDialog);
  private readonly confirm = inject(ConfirmService);
  private readonly snackbar = inject(MatSnackBar);

  readonly columns = ['date', 'description', 'category', 'amount', 'actions'];

  readonly expenses = signal<Expense[]>([]);
  readonly categories = signal<Category[]>([]);
  readonly total = signal(0);
  readonly defaultCurrency = signal('USD');
  readonly loading = signal(false);

  readonly monthControl = new FormControl<Date | null>(null);
  readonly categoryControl = new FormControl<number | null>(null);

  constructor() {
    this.categoryService
      .getAll()
      .pipe(takeUntilDestroyed())
      .subscribe((categories) => this.categories.set(categories));

    this.categoryControl.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.load());
    this.monthControl.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.load());

    this.load();
  }

  load(): void {
    const month = this.monthControl.value;
    const defaultCurrency = this.currencyService.defaultCurrency;

    this.loading.set(true);
    this.expenseService
      .getAll({
        categoryId: this.categoryControl.value ?? undefined,
        year: month ? month.getFullYear() : undefined,
        month: month ? month.getMonth() + 1 : undefined,
      })
      .pipe(
        combineLatestWith(this.currencyService.getCurrencies())
      )
      .pipe(takeUntilDestroyed())
      .subscribe(([expenses, currency]) => {
        this.expenses.set(expenses);
        this.defaultCurrency.set(currency.defaultCurrency);
        this.total.set(expenses.reduce((sum, e) => sum + e.amount, 0));
        this.loading.set(false);
      });
  }

  clearFilters(): void {
    this.monthControl.setValue(null);
    this.categoryControl.setValue(null);
  }

  openDialog(expense?: Expense): void {
    const ref = this.dialog.open(ExpenseDialog, {
      data: { expense, categories: this.categories() },
      width: '480px',
    });

    ref
      .afterClosed()
      .pipe(takeUntilDestroyed())
      .subscribe((payload) => {
        if (!payload) return;
        if (expense) {
          this.expenseService.update(expense.id, payload).subscribe(() => this.load());
        } else {
          this.expenseService.create(payload).subscribe(() => this.load());
        }
      });
  }

  remove(expense: Expense): void {
    this.confirm
      .confirm({
        title: 'Delete expense',
        message: `Delete "${expense.description}" (${expense.amount} ${expense.currencyCode})? This cannot be undone.`,
        confirmLabel: 'Delete',
      })
      .pipe(takeUntilDestroyed())
      .subscribe((confirmed) => {
        if (!confirmed) return;
        this.expenseService.delete(expense.id).subscribe({
          next: () => {
            this.snackbar.open('Expense deleted', 'Close', { duration: 3000 });
            this.load();
          },
          error: () => {
            this.snackbar.open('Could not delete this expense.', 'Close', { duration: 4000 });
          },
        });
      });
  }

  formatDate(value: string): string {
    return new Date(value).toLocaleDateString();
  }
}
