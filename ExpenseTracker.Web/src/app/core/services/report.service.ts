import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CategoryTotal, DashboardSummary, MonthTotal, StatusTotal } from '../models/report';

@Injectable({ providedIn: 'root' })
export class ReportService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/reports`;

  getSummary(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${this.base}/summary`);
  }

  getExpensesByCategory(): Observable<CategoryTotal[]> {
    return this.http.get<CategoryTotal[]>(`${this.base}/expenses-by-category`);
  }

  getExpensesByMonth(months = 12): Observable<MonthTotal[]> {
    return this.http.get<MonthTotal[]>(`${this.base}/expenses-by-month`, {
      params: new HttpParams().set('months', months),
    });
  }

  getInvoicesByMonth(months = 12): Observable<MonthTotal[]> {
    return this.http.get<MonthTotal[]>(`${this.base}/invoices-by-month`, {
      params: new HttpParams().set('months', months),
    });
  }

  getInvoicesByStatus(): Observable<StatusTotal[]> {
    return this.http.get<StatusTotal[]>(`${this.base}/invoices-by-status`);
  }
}
