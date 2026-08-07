export interface AuthResponse {
  id: number;
  username: string;
  email: string;
  displayName: string;
  currencyCode: string;
  token: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  displayName?: string;
}
