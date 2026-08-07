import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Expense, ExpenseQuery, ExpenseRequest } from '../models/expense';

@Injectable({ providedIn: 'root' })
export class ExpenseService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/expenses`;

  getAll(query: ExpenseQuery = {}): Observable<Expense[]> {
    let params = new HttpParams();
    if (query.categoryId != null) params = params.set('categoryId', query.categoryId);
    if (query.year != null) params = params.set('year', query.year);
    if (query.month != null) params = params.set('month', query.month);
    return this.http.get<Expense[]>(this.base, { params });
  }

  getById(id: number): Observable<Expense> {
    return this.http.get<Expense>(`${this.base}/${id}`);
  }

  create(payload: ExpenseRequest): Observable<Expense> {
    return this.http.post<Expense>(this.base, payload);
  }

  update(id: number, payload: ExpenseRequest): Observable<Expense> {
    return this.http.put<Expense>(`${this.base}/${id}`, payload);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
