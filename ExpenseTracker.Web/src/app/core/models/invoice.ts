export type InvoiceStatus = 'Draft' | 'Sent' | 'Paid' | 'Overdue' | 'Cancelled';
export type PaymentMethod = 'Cash' | 'Bank' | 'Card' | 'Other';

export interface Invoice {
  id: number;
  invoiceNumber: string;
  customerId: number | null;
  customerName: string | null;
  issueDate: string;
  dueDate: string;
  status: InvoiceStatus;
  currencyCode: string;
  taxRate: number;
  subtotal: number;
  tax: number;
  total: number;
  paidAmount: number;
  balance: number;
}

export interface InvoiceItem {
  id: number;
  description: string;
  quantity: number;
  unitPrice: number;
  amount: number;
}

export interface InvoicePayment {
  id: number;
  amount: number;
  paymentDate: string;
  method: PaymentMethod;
  reference: string | null;
}

export interface InvoiceDetail extends Invoice {
  notes: string | null;
  items: InvoiceItem[];
  payments: InvoicePayment[];
}

export interface InvoiceItemRequest {
  description: string;
  quantity: number;
  unitPrice: number;
}

export interface InvoiceRequest {
  customerId?: number | null;
  issueDate: string;
  dueDate: string;
  status: 'Draft' | 'Sent';
  taxRate: number;
  currencyCode: string;
  notes?: string | null;
  items: InvoiceItemRequest[];
}

export interface InvoiceQuery {
  status?: string;
  customerId?: number;
}

export interface PaymentRequest {
  amount: number;
  paymentDate: string;
  method: PaymentMethod;
  reference?: string | null;
}
