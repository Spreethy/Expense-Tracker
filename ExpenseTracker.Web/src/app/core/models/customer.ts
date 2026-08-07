export interface Customer {
  id: number;
  name: string;
  email: string;
  phone: string;
  address: string;
  createdAt: string;
}

export interface CustomerRequest {
  name: string;
  email: string;
  phone: string;
  address: string;
}
