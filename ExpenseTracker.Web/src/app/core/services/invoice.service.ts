import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Invoice,
  InvoiceDetail,
  InvoiceQuery,
  InvoiceRequest,
  InvoiceStatus,
  PaymentRequest,
} from '../models/invoice';

@Injectable({ providedIn: 'root' })
export class InvoiceService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/invoices`;

  getAll(query: InvoiceQuery = {}): Observable<Invoice[]> {
    let params = new HttpParams();
    if (query.status) params = params.set('status', query.status);
    if (query.customerId != null) params = params.set('customerId', query.customerId);
    return this.http.get<Invoice[]>(this.base, { params });
  }

  getById(id: number): Observable<InvoiceDetail> {
    return this.http.get<InvoiceDetail>(`${this.base}/${id}`);
  }

  create(payload: InvoiceRequest): Observable<InvoiceDetail> {
    return this.http.post<InvoiceDetail>(this.base, payload);
  }

  update(id: number, payload: InvoiceRequest): Observable<InvoiceDetail> {
    return this.http.put<InvoiceDetail>(`${this.base}/${id}`, payload);
  }

  updateStatus(id: number, status: InvoiceStatus): Observable<InvoiceDetail> {
    return this.http.patch<InvoiceDetail>(`${this.base}/${id}/status`, { status });
  }

  addPayment(id: number, payload: PaymentRequest): Observable<InvoiceDetail> {
    return this.http.post<InvoiceDetail>(`${this.base}/${id}/payments`, payload);
  }

  removePayment(id: number, paymentId: number): Observable<InvoiceDetail> {
    return this.http.delete<InvoiceDetail>(`${this.base}/${id}/payments/${paymentId}`);
  }

  getPdf(id: number): Observable<Blob> {
    return this.http.get(`${this.base}/${id}/pdf`, { responseType: 'blob' });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
