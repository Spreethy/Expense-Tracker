import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./features/auth/login/login').then((m) => m.Login) },
  { path: 'register', loadComponent: () => import('./features/auth/register/register').then((m) => m.Register) },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.Dashboard),
  },
  {
    path: 'expenses',
    canActivate: [authGuard],
    loadComponent: () => import('./features/expenses/expenses').then((m) => m.Expenses),
  },
  {
    path: 'customers',
    canActivate: [authGuard],
    loadComponent: () => import('./features/customers/customers').then((m) => m.Customers),
  },
  {
    path: 'categories',
    canActivate: [authGuard],
    loadComponent: () => import('./features/categories/categories').then((m) => m.Categories),
  },
  {
    path: 'invoices',
    canActivate: [authGuard],
    loadComponent: () => import('./features/invoices/invoices').then((m) => m.Invoices),
  },
  {
    path: 'invoices/new',
    canActivate: [authGuard],
    loadComponent: () => import('./features/invoices/invoice-editor/invoice-editor').then((m) => m.InvoiceEditor),
  },
  {
    path: 'invoices/:id',
    canActivate: [authGuard],
    loadComponent: () => import('./features/invoices/invoice-detail/invoice-detail').then((m) => m.InvoiceDetail),
  },
  {
    path: 'invoices/:id/edit',
    canActivate: [authGuard],
    loadComponent: () => import('./features/invoices/invoice-editor/invoice-editor').then((m) => m.InvoiceEditor),
  },
  {
    path: 'reports',
    canActivate: [authGuard],
    loadComponent: () => import('./features/reports/reports').then((m) => m.Reports),
  },
  {
    path: 'settings',
    canActivate: [authGuard],
    loadComponent: () => import('./features/settings/settings').then((m) => m.Settings),
  },
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: '**', redirectTo: 'dashboard' },
];
