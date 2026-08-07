export interface DashboardSummary {
  totalExpensesThisMonth: number;
  totalExpensesAllTime: number;
  totalInvoiced: number;
  totalPaid: number;
  totalOutstanding: number;
  expenseCount: number;
  invoiceCount: number;
  overdueInvoiceCount: number;
  customerCount: number;
  currency: string;
}

export interface CategoryTotal {
  categoryId: number | null;
  category: string;
  amount: number;
  count: number;
}

export interface MonthTotal {
  month: string;
  amount: number;
  count: number;
}

export interface StatusTotal {
  status: string;
  amount: number;
  count: number;
}
