import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Customer } from '../../../core/models/customer';

@Component({
  selector: 'app-customer-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
  ],
  templateUrl: './customer-dialog.html',
  styleUrl: './customer-dialog.scss',
})
export class CustomerDialog {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<CustomerDialog>);
  private readonly data = inject<Customer | undefined>(MAT_DIALOG_DATA);

  protected readonly isEdit = !!this.data;

  protected readonly form = this.fb.nonNullable.group({
    name: [this.data?.name ?? '', [Validators.required, Validators.maxLength(150)]],
    email: [this.data?.email ?? '', [Validators.email, Validators.maxLength(150)]],
    phone: [this.data?.phone ?? '', [Validators.maxLength(30)]],
    address: [this.data?.address ?? '', [Validators.maxLength(300)]],
  });

  protected save(): void {
    if (this.form.invalid) {
      return;
    }
    const value = this.form.getRawValue();
    this.dialogRef.close({
      ...value,
      email: value.email || null,
      phone: value.phone || null,
      address: value.address || null,
    });
  }
}
