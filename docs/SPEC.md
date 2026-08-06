# Expense Tracking / Invoice Management System — Specification & Implementation Plan

> Status: **Draft — awaiting confirmation**
> Stack: Angular + .NET Web API (net9.0) + SQL Server
> Owner: E:\Project

---

## 1. Overview

A personal/small-business web application for tracking expenses and managing customer invoices.
Users register, log in, and manage **their own** data (single-role model). The app provides full CRUD
for expenses, customers, invoices, and expense categories, plus a reporting dashboard with charts
and a PDF export of invoices.

The `.NET Web API` scaffold already exists in `ExpenseTracker.Api/` (auth, JWT, EF Core, controllers,
seed data). The Angular frontend (`ExpenseTracker.Web/`) does not exist yet and will be built from scratch.

## 2. Goals

- Multi-user, per-user data isolation (JWT auth, single role).
- CRUD for: Expenses, Customers, Invoices (+ line items), Categories.
- Invoice workflow: Draft → Sent → Paid / Overdue / Cancelled, with payment tracking.
- Auto invoice numbering per user (e.g. `INV-2026-0001`).
- PDF export of an invoice.
- Reporting dashboard: summary cards + charts (expenses by category / month, invoices by status / month).
- Multi-currency support (per-record currency, conversion for reports).

## 3. Non-Goals (v1)

- Recurring invoices (deferred to v2).
- Email delivery of invoices (deferred to v2).
- Online payment gateway (Stripe/PayPal pay-by-link) — v1 is **manual payment tracking only**; customers pay externally (bank transfer/cash) and the user records the payment in the app. Deferred to v2.
- Admin/role management screens (deferred to v2).
- Mobile apps, offline mode, audit logs.

## 4. Confirmed Decisions (from discovery)

| Area            | Decision                                                            |
|-----------------|---------------------------------------------------------------------|
| UI library      | Angular Material                                                     |
| Charts          | ngx-charts (D3 based, Angular-native)                                |
| Auth / roles    | Single role; every user owns and sees only their data                |
| Database (dev)  | SQL Server 2022 in Docker (docker-compose); LocalDB as fallback      |
| Invoice extras  | Auto numbering, payment tracking, PDF export                         |
| Categories      | User-managed with seeded presets                                     |
| Currency        | Multi-currency: per-record code, reports converted to user default   |

## 5. Tech Stack

### Backend — `ExpenseTracker.Api/` (exists, will be extended)
- ASP.NET Core Web API, net9.0
- Entity Framework Core 9 + SQL Server provider
- JWT Bearer auth (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- Swagger / OpenAPI (`Swashbuckle.AspNetCore`)
- `QuestPDF` for invoice PDF generation (new dependency)
- Money stored as `decimal(18,2)`; rates as `decimal(18,8)`

### Frontend — `ExpenseTracker.Web/` (new)
- Angular (latest, Angular 19/20) + Angular CLI
- Angular Material (components, theming, forms)
- Angular Router with auth guard
- Reactive Forms for all edit screens
- ngx-charts for charts
- HTTP interceptor to attach JWT; error handling

### Infra
- `docker-compose.yml` at repo root: `mcr.microsoft.com/mssql/server:2022-latest`
- Dev URLs: API `http://localhost:5000`, Angular `http://localhost:4200`

## 6. Repository Layout

```
E:\Project\
├─ .gitignore
├─ docker-compose.yml            # SQL Server 2022 for dev
├─ README.md
├─ docs/
│  ├─ SPEC.md                   # this document
│  └─ API.md                    # endpoint reference (generated/maintained during dev)
├─ ExpenseTracker.Api/          # existing .NET Web API
└─ ExpenseTracker.Web/          # new Angular app
```

## 7. Data Model

Current entities (already implemented): `User`, `Customer`, `Expense`, `Invoice`, `InvoiceItem`.

### New / changed entities

- **Category** (new)
  - Id, UserId, Name, Color (optional), CreatedAt
  - Seeded presets: Travel, Meals, Office Supplies, Software, Utilities, Other
  - `Expense.Category` → refactor from free-text string to `CategoryId` FK (keep category name denormalized for reports? No — resolve via join).

- **User** (changed)
  - add `CurrencyCode` (default `USD`)

- **Expense** (changed)
  - replace `Category` string with `CategoryId` (FK, optional → falls back to "Other")
  - add `CurrencyCode` (default = user default)

- **Invoice** (changed)
  - add `CurrencyCode` (default = user default)
  - add `PaymentDate`? No — payments are a separate child table.

- **InvoicePayment** (new)
  - Id, InvoiceId, Amount, PaymentDate, Method (enum: Cash/Bank/Card/Other), Reference, CreatedAt
  - Manual tracking only: the user records payments received externally; there is **no** online gateway in v1.
  - Status derivation: `Paid` when `Sum(payments) >= Total`; `Sent` + due date passed → `Overdue`; otherwise explicit status.
  - **Note:** existing `InvoiceStatus.Paid` stays, but UI/server computes effective paid amount.

- **CurrencyRate** (new)
  - Id, UserId, FromCurrency (3-letter ISO), RateToDefault (decimal), UpdatedAt
  - Used to convert a record's amount into the user's default currency for reports.
  - Seeded for the user's default currency at 1.0; other rates entered via UI or left at 1.0.

### Key relationships (already in DbContext)
- User 1—N Customer, Expense, Invoice, Category (cascade delete)
- Customer 1—N Invoice (SetNull on delete)
- Invoice 1—N InvoiceItem (cascade), 1—N InvoicePayment (cascade)
- Unique index: `(UserId, InvoiceNumber)` to guarantee invoice number uniqueness.

## 8. API Design

All routes under `api/`; protected with `[Authorize]` except `register` / `login`. All queries filter by the authenticated user.

| Method | Route                                  | Description                                        |
|--------|----------------------------------------|----------------------------------------------------|
| POST   | /api/auth/register                     | Create account, return JWT                         |
| POST   | /api/auth/login                        | Log in, return JWT                                 |
| GET    | /api/auth/me                           | Current user profile                               |
| GET    | /api/categories                        | List categories (seeded presets first time)        |
| POST   | /api/categories                        | Create category                                    |
| PUT    | /api/categories/{id}                   | Update category                                    |
| DELETE | /api/categories/{id}                   | Delete category (expenses → Other)                 |
| GET    | /api/expenses                          | List (filter: categoryId, month/from/to)           |
| POST   | /api/expenses                          | Create expense                                     |
| GET    | /api/expenses/{id}                     | Get expense                                        |
| PUT    | /api/expenses/{id}                     | Update expense                                     |
| DELETE | /api/expenses/{id}                     | Delete expense                                     |
| GET    | /api/customers                         | List customers                                     |
| POST   | /api/customers                         | Create customer                                    |
| GET    | /api/customers/{id}                    | Get customer                                       |
| PUT    | /api/customers/{id}                    | Update customer                                    |
| DELETE | /api/customers/{id}                    | Delete customer                                    |
| GET    | /api/invoices                          | List (filter: status, customerId, month)           |
| POST   | /api/invoices                          | Create invoice (auto invoice number assigned)      |
| GET    | /api/invoices/{id}                     | Get invoice with items + payments + customer       |
| PUT    | /api/invoices/{id}                     | Update invoice (incl. line items)                  |
| DELETE | /api/invoices/{id}                     | Delete invoice                                     |
| PATCH  | /api/invoices/{id}/status              | Transition status (server-validated rules)         |
| POST   | /api/invoices/{id}/payments            | Record a payment                                   |
| DELETE | /api/invoices/{id}/payments/{pid}      | Remove a payment                                   |
| GET    | /api/invoices/{id}/pdf                 | Download PDF (application/pdf)                     |
| GET    | /api/reports/summary                   | Dashboard summary cards (converted to default cur) |
| GET    | /api/reports/expenses-by-category      | Pie data, converted                                |
| GET    | /api/reports/expenses-by-month         | Line/bar data, converted                           |
| GET    | /api/reports/invoices-by-month         | Line/bar data, converted                           |
| GET    | /api/reports/invoices-by-status        | Status breakdown, converted                        |
| GET    | /api/currencies                        | Supported ISO currency list + user default         |
| PUT    | /api/currencies/rates                  | Update FX rates to user default                    |

**Status transition rules (server-enforced):**
- Draft → Sent / Cancelled
- Sent → Paid (allowed if payments ≥ total) / Overdue (auto) / Cancelled
- Paid → none (locked)
- Cancelled → none (locked)
- Invoice content is **editable only while Draft**.

**Invoice numbering service:**
- Format `INV-YYYY-####`, sequential per user per year, allocated inside a transaction
  (row lock on a per-user counter) and guarded by the unique index.

**Error format:** consistent `{ "message": "..." }` (already used) plus 400 for validation.

## 9. Frontend Structure

```
ExpenseTracker.Web/
├─ src/app/
│  ├─ core/                 # auth service, guards, HTTP interceptor, api client, env config
│  ├─ shared/               # material theme, common components (currency pipe, confirm dialog, empty state)
│  ├─ features/
│  │  ├─ auth/              # login, register
│  │  ├─ dashboard/         # summary cards + charts
│  │  ├─ expenses/          # list + form
│  │  ├─ categories/        # management page
│  │  ├─ customers/         # list + form
│  │  ├─ invoices/          # list, detail, editor (line items), payments, PDF download
│  │  └─ reports/           # charts pages
│  │  └─ settings/          # default currency + FX rates
│  └─ app.routes.ts         # lazy-loaded feature routes, auth guard
```

**Routes:**
- `/login`, `/register`
- `/dashboard` (default after login)
- `/expenses`, `/expenses/new`, `/expenses/:id`
- `/categories`
- `/customers`, `/customers/new`, `/customers/:id`
- `/invoices`, `/invoices/new`, `/invoices/:id`
- `/reports`
- `/settings`

**Global behaviors:**
- 401 → redirect to login; 400/409 → inline validation/error snackbar; 404 → not-found page.
- Amounts displayed with currency code + symbol; report amounts in user's default currency.

## 10. Multi-Currency Design (v1 pragmatic)

- Every `Expense`/`Invoice` carries an ISO `CurrencyCode` (defaulted to the user's default).
- Reports convert each amount to the user's default currency using per-user `CurrencyRate`.
  If no rate is stored for a currency, treat as 1.0 (documented limitation) and flag in UI.
- Users manage their default currency + rates in Settings.
- No automatic FX feeds in v1 (v2 idea).

## 11. Non-Functional / Quality

- Server-side validation on all writes; decimal precision `(18,2)` for money.
- Passwords hashed (existing `PasswordHasher`), JWT secret from config (moved to env in production).
- SQL injection / EF parameterization by default; no secrets committed.
- CORS policy restricted to `http://localhost:4200` in production config.
- Dockerized SQL Server for parity between dev and CI.
- Unit/integration tests for: invoice numbering, status transitions, payment settlement,
  currency conversion, auth. Frontend: unit tests for key services + a smoke E2E happy path.

## 12. Development Setup (Phase 0 deliverable)

- `docker-compose up -d db` → SQL Server 2022 on `localhost:1433`
  (`SA` password via `.env`, connection string in appsettings.Development.json).
- `dotnet ef migrations add Init` + `dotnet ef database update`.
- API run: `dotnet run --project ExpenseTracker.Api` → `http://localhost:5000` (Swagger at `/swagger`).
- Web run: `ng serve` in `ExpenseTracker.Web` → `http://localhost:4200`.
- Seed user: `demo / Demo123!`.

## 13. Phased Implementation Plan

Each phase ends with a commit (or a small series of commits) and the app in a working state.
Details of each phase may be expanded into `docs/PHASES.md` as they start.

### Phase 0 — Repo & infra baseline
- `git init`, root `.gitignore` (.NET + Node), commit current API as baseline.
- `docker-compose.yml` (SQL Server 2022), `.env.example`.
- Initial EF migration + `database update`.
- Verify API builds and runs against Docker SQL Server; Swagger reachable.
- **Done when:** `docker compose up` + `dotnet run` yields a working API with seeded data.

### Phase 1 — API data model & invoice features
- Add `Category`, `InvoicePayment`, `CurrencyRate` entities + `User.CurrencyCode`; refactor `Expense` to `CategoryId` + `CurrencyCode`; add `Invoice.CurrencyCode`.
- Add `QuestPDF`; implement invoice PDF generation endpoint.
- Implement invoice numbering service (transactional, per-user sequential).
- Implement payment endpoints + effective-status logic (settled/overdue) in reports.
- Categories CRUD + seed presets; currencies/rates endpoints.
- Currency conversion in all report endpoints.
- Update/expand existing DTOs and controllers; integration tests for numbering/status/payment/conversion.
- **Done when:** all v1 API endpoints present, Swagger documents them, tests green.

### Phase 2 — Angular scaffold + auth shell
- `ng new ExpenseTracker.Web` (SCSS, standalone components, router).
- Add Angular Material + ngx-charts; global theme + shared pipes/dialogs.
- Core: API client, JWT interceptor, auth service, route guard.
- Login/register pages wired to API; 401 handling; route redirection.
- **Done when:** can register/login/logout against the API; guarded routes work.

### Phase 3 — Core CRUD: Categories, Customers, Expenses
- Categories management page.
- Customers list + create/edit/delete.
- Expenses list (filters: category, month) + create/edit/delete.
- **Done when:** full CRUD works end-to-end for all three against the API.

### Phase 4 — Invoices
- Invoices list (filters: status, customer) with status chips.
- Invoice editor: header (customer, dates, tax, currency, notes) + line items grid.
- Invoice detail: items, totals, payments list, add/remove payment, status transitions, PDF download.
- **Done when:** create/read/update + payments + status + PDF all work; paid lock rules enforced.

### Phase 5 — Dashboard & Reports
- Dashboard: summary cards (reports/summary) + ngx-charts (expenses by month, invoices by status).
- Reports page: all report endpoints visualized.
- Settings: default currency + FX rates.
- **Done when:** all charts render from live data, conversion applied.

### Phase 6 — Polish & hardening
- Validation UX, consistent error snackbars, loading states, empty states.
- Seeding completeness, README with setup instructions, `docs/API.md` reference.
- Build/lint/format checks (`ng build`, `dotnet build`), test suite run, final review.
- **Done when:** clean build, tests pass, README documents full setup.

## 14. Open Questions / Assumptions

- **Currency symbol map:** we assume a small static map (USD $, EUR €, GBP £, INR ₹, …); free-form codes render as the code alone.
- **Payment flow (v1):** customer pays externally (bank transfer/cash/check). User records the payment on the invoice (amount, date, method, reference). Invoice shows `Paid` once payments ≥ total. Online pay-by-link is a v2 candidate.
- **PDF design:** default clean template (logo placeholder, company=user display name, customer, line items, tax, totals); no user-uploaded logo in v1.
- **Deletion policy:** expenses/customers/invoices hard-delete (user-confirmed in UI) for v1; soft-delete is a v2 candidate.
- **Rates source:** manual entry only in v1.
- Anything else worth pinning down before we start implementation?

## 15. Timeline Estimate (rough)

| Phase | Scope | Est. effort |
|-------|-------|-------------|
| 0 | Repo/infra baseline | 0.5 day |
| 1 | API data model + invoice features | 2–3 days |
| 2 | Angular scaffold + auth | 1–2 days |
| 3 | Core CRUD screens | 1–2 days |
| 4 | Invoices UI | 2 days |
| 5 | Dashboard + reports | 1–2 days |
| 6 | Polish & hardening | 1 day |
| **Total** | | **~9–12 days** |

---

_Next step: awaiting confirmation, then Phase 0 begins with a git baseline commit._
