import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Customer, CustomerRequest } from '../models/customer';

@Injectable({ providedIn: 'root' })
export class CustomerService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/customers`;

  getAll(): Observable<Customer[]> {
    return this.http.get<Customer[]>(this.base);
  }

  getById(id: number): Observable<Customer> {
    return this.http.get<Customer>(`${this.base}/${id}`);
  }

  create(payload: CustomerRequest): Observable<Customer> {
    return this.http.post<Customer>(this.base, payload);
  }

  update(id: number, payload: CustomerRequest): Observable<Customer> {
    return this.http.put<Customer>(`${this.base}/${id}`, payload);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
