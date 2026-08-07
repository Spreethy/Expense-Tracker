# Expense Tracker

A personal / small-business expense tracking and invoice management web application with full CRUD for
expenses, customers, invoices, and categories, plus a reporting dashboard with charts, multi-currency
support, and PDF invoice export.

**Stack:** Angular 20 (frontend) · ASP.NET Core 9 Web API (backend) · SQL Server 2022 (database) · Docker

## Features

- **Auth** — register / login with JWT; every user sees only their own data.
- **Expenses** — full CRUD with category and month filters, multi-currency amounts, running totals.
- **Customers** — full CRUD.
- **Categories** — user-managed with seeded presets; expenses fall back to "Other" when a category is deleted.
- **Invoices** — draft → sent → paid / overdue / cancelled workflow with auto numbering
  (`INV-YYYY-####`), line items, tax, payment tracking, and PDF export.
- **Payments** — manual payment recording (cash / bank / card / other); invoices auto-settle when
  payments cover the balance.
- **Dashboard & Reports** — summary cards plus charts (expenses by category/month, invoices by
  status/month) rendered with ngx-charts; all amounts converted to your default currency.
- **Settings** — choose your default currency and manage exchange rates (rates are used to convert
  amounts for reports).

## Repository layout

```
ExpenseTracker.Api/          # .NET 9 Web API (auth, EF Core, controllers, services)
ExpenseTracker.Api.Tests/    # xUnit tests (invoice numbering, workflow, currency, PDF)
ExpenseTracker.Web/          # Angular 20 SPA (Material, ngx-charts, JWT)
docs/                        # SPEC.md + API.md (local documentation)
docker-compose.yml           # SQL Server 2022 for development
.env.example                 # database password template
```

## Prerequisites

- [Node.js](https://nodejs.org) 20.19+ (project developed on 24.x)
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for SQL Server)
- (Optional) `dotnet-ef` global tool for migrations:
  ```powershell
  dotnet tool install --global dotnet-ef
  ```

## Getting started

### 1. Start the database

```powershell
# create .env from the template (set the SA password you want to use)
Copy-Item .env.example .env

docker compose up -d db
```

### 2. Run the API

```powershell
# development connection string lives in appsettings.Development.json;
# it points at localhost,1433 with the SA password from .env
dotnet run --project ExpenseTracker.Api
```

The API applies pending EF migrations and seeds demo data on startup.
It serves at `http://localhost:5000` (Swagger at `http://localhost:5000/swagger`).

### 3. Run the web app

```powershell
cd ExpenseTracker.Web
npm install
npm start        # or: ng serve
```

The SPA serves at `http://localhost:4200`. The API URL is configured in
`src/environments/environment.ts`.

### 4. Log in

| | |
|---|---|
| Username | `demo` |
| Password | `Demo123!` |

The demo account includes seeded categories, 4 customers, 60 expenses, and 24 invoices.

## Common tasks

```powershell
# Rebuild database from scratch (re-seeds demo data)
docker compose down -v
docker compose up -d db
# restart the API afterward

# Run backend tests
dotnet test ExpenseTracker.Api.Tests

# Run frontend unit tests
cd ExpenseTracker.Web
npm test -- --watch=false

# Production build of the frontend
cd ExpenseTracker.Web
npm run build

# EF migration (after changing models)
dotnet ef migrations add <Name> --project ExpenseTracker.Api
dotnet ef database update --project ExpenseTracker.Api
```

## API overview

All endpoints are under `/api` and protected by JWT (except `register`/`login`). Full reference:
see [`docs/API.md`](docs/API.md).

## Notes

- The database is SQL Server 2022 running in Docker for development parity.
- Money is stored as `decimal(18,2)`; exchange rates as `decimal(18,8)`.
- QuestPDF is used for PDF generation under the Community license.
- CORS currently allows all origins for development convenience; restrict it before production.
