export interface CurrencyInfo {
  code: string;
  name: string;
  symbol: string;
  rateToDefault: number;
}

export interface UserCurrency {
  defaultCurrency: string;
  currencies: CurrencyInfo[];
}
