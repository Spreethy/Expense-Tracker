export interface Category {
  id: number;
  name: string;
  color: string | null;
  expenseCount: number;
}

export interface CategoryRequest {
  name: string;
  color?: string;
}
