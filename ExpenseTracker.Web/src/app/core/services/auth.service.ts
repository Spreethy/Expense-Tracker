import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom, Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, RegisterRequest } from '../models/auth';

const TOKEN_KEY = 'et_token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  readonly currentUser = signal<AuthResponse | null>(null);
  readonly isAuthenticated = computed(() => this.currentUser() !== null);

  /** Restore the session on app boot. Must run AFTER construction (APP_INITIALIZER),
   *  otherwise fetchMe() would trigger the auth interceptor's inject(AuthService)
   *  while AuthService is still mid-construction -> circular DI error. */
  initialize(): Promise<void> {
    if (!this.token) return Promise.resolve();
    return firstValueFrom(this.fetchMe())
      .then(() => undefined)
      .catch(() => {
        localStorage.removeItem(TOKEN_KEY);
        this.currentUser.set(null);
      });
  }

  get token(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  login(username: string, password: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/api/auth/login`, { username, password })
      .pipe(tap((res) => this.setAuth(res)));
  }

  register(payload: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/api/auth/register`, payload)
      .pipe(tap((res) => this.setAuth(res)));
  }

  fetchMe(): Observable<AuthResponse> {
    return this.http
      .get<AuthResponse>(`${environment.apiUrl}/api/auth/me`)
      .pipe(tap((res) => this.setAuth(res)));
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    this.currentUser.set(null);
    this.router.navigate(['/login']);
  }

  private setAuth(res: AuthResponse): void {
    localStorage.setItem(TOKEN_KEY, res.token);
    this.currentUser.set(res);
  }
}
