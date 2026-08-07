import { Pipe, PipeTransform } from '@angular/core';

const SYMBOLS: Record<string, string> = {
  USD: '$',
  EUR: '€',
  GBP: '£',
  INR: '₹',
  CAD: 'C$',
  AUD: 'A$',
  JPY: '¥',
  CNY: '¥',
  SGD: 'S$',
  AED: 'د.إ',
};

@Pipe({ name: 'money' })
export class MoneyPipe implements PipeTransform {
  transform(value: number | null | undefined, currencyCode?: string | null): string {
    const amount = value ?? 0;
    const code = currencyCode?.toUpperCase() ?? 'USD';
    const symbol = SYMBOLS[code] ?? '';
    const formatted = amount.toLocaleString(undefined, {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    });
    return `${symbol}${formatted} ${code}`;
  }
}
