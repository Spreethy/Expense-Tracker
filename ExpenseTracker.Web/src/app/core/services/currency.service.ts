import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, shareReplay, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { UserCurrency } from '../models/currency';

@Injectable({ providedIn: 'root' })
export class CurrencyService {
  private readonly http = inject(HttpClient);
  private cached: UserCurrency | null = null;

  getCurrencies(force = false): Observable<UserCurrency> {
    if (this.cached && !force) {
      return new Observable((sub) => {
        sub.next(this.cached!);
        sub.complete();
      });
    }
    return this.http
      .get<UserCurrency>(`${environment.apiUrl}/api/currencies`)
      .pipe(
        tap((res) => (this.cached = res)),
        shareReplay({ bufferSize: 1, refCount: false })
      );
  }

  get defaultCurrency(): string {
    return this.cached?.defaultCurrency ?? '';
  }
}
