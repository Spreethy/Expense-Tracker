import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Category } from '../../../core/models/category';

const PRESET_COLORS = ['#1976d2', '#43a047', '#f9a825', '#e53935', '#8e24aa', '#00897b', '#5e35b1', '#6d4c41'];

@Component({
  selector: 'app-category-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
  ],
  templateUrl: './category-dialog.html',
  styleUrl: './category-dialog.scss',
})
export class CategoryDialog {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<CategoryDialog>);
  private readonly data = inject<Category | undefined>(MAT_DIALOG_DATA);

  protected readonly presets = PRESET_COLORS;
  protected readonly isEdit = !!this.data;

  protected readonly form = this.fb.nonNullable.group({
    name: [this.data?.name ?? '', [Validators.required, Validators.maxLength(50)]],
    color: [this.data?.color ?? PRESET_COLORS[0], [Validators.maxLength(20)]],
  });

  protected save(): void {
    if (this.form.invalid) {
      return;
    }
    this.dialogRef.close(this.form.getRawValue());
  }
}
