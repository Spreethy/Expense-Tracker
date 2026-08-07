import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-register',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSnackBarModule,
  ],
  templateUrl: './register.html',
  styleUrl: './register.scss',
})
export class Register {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly snackbar = inject(MatSnackBar);

  protected readonly submitting = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    username: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(50)]],
    email: ['', [Validators.required, Validators.email]],
    displayName: ['', [Validators.maxLength(100)]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  protected submit(): void {
    if (this.form.invalid) {
      return;
    }

    const { username, email, displayName, password } = this.form.getRawValue();
    this.submitting.set(true);

    this.auth.register({ username, email, displayName, password }).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: (err) => {
        this.submitting.set(false);
        const message =
          err?.error?.message ?? 'Registration failed. Please try again.';
        this.snackbar.open(message, 'Close', { duration: 5000 });
      },
    });
  }
}
