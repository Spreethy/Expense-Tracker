import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { Category } from '../../core/models/category';
import { CategoryService } from '../../core/services/category.service';
import { EmptyState } from '../../shared/empty-state/empty-state';
import { ConfirmService } from '../../shared/services/confirm.service';
import { CategoryDialog } from './category-dialog/category-dialog';

@Component({
  selector: 'app-categories',
  imports: [
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressBarModule,
    MatTableModule,
    EmptyState,
  ],
  templateUrl: './categories.html',
  styleUrl: './categories.scss',
})
export class Categories {
  private readonly categoryService = inject(CategoryService);
  private readonly dialog = inject(MatDialog);
  private readonly confirm = inject(ConfirmService);
  private readonly snackbar = inject(MatSnackBar);

  readonly columns = ['color', 'name', 'expenses', 'actions'];
  readonly categories = signal<Category[]>([]);
  readonly loading = signal(false);

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.categoryService
      .getAll()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: (categories) => this.categories.set(categories),
        error: () => this.onLoadError(),
        complete: () => this.loading.set(false),
      });
  }

  private onLoadError(): void {
    this.snackbar.open('Could not load categories.', 'Close', { duration: 4000 });
  }

  openDialog(category?: Category): void {
    const ref = this.dialog.open(CategoryDialog, {
      data: category,
      width: '420px',
    });

    ref
      .afterClosed()
      .pipe(takeUntilDestroyed())
      .subscribe((payload) => {
        if (!payload) return;
        const request = category
          ? this.categoryService.update(category.id, payload)
          : this.categoryService.create(payload);
        request.subscribe({
          next: () => {
            this.snackbar.open(category ? 'Category updated' : 'Category created', 'Close', { duration: 3000 });
            this.load();
          },
          error: (err) => {
            this.snackbar.open(err.error?.message ?? 'Could not save the category.', 'Close', { duration: 4000 });
          },
        });
      });
  }

  remove(category: Category): void {
    this.confirm
      .confirm({
        title: 'Delete category',
        message: `Delete "${category.name}"? Existing expenses will be kept but become uncategorized.`,
        confirmLabel: 'Delete',
      })
      .pipe(takeUntilDestroyed())
      .subscribe((confirmed) => {
        if (!confirmed) return;
        this.categoryService.delete(category.id).subscribe(() => {
          this.snackbar.open('Category deleted', 'Close', { duration: 3000 });
          this.load();
        });
      });
  }
}
