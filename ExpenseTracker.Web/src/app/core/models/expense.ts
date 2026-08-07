export interface Expense {
  id: number;
  description: string;
  categoryId: number | null;
  category: string;
  amount: number;
  currencyCode: string;
  expenseDate: string;
  notes: string | null;
}

export interface ExpenseRequest {
  description: string;
  categoryId?: number | null;
  amount: number;
  currencyCode: string;
  expenseDate: string;
  notes?: string | null;
}

export interface ExpenseQuery {
  categoryId?: number;
  year?: number;
  month?: number;
}
