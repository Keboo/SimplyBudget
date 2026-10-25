# Project Context

- **Owner:** Kevin Bost
- **Project:** SimplyBudget — personal budgeting and expense tracking, including pending expenses and receipt uploads.
- **Stack:** C#, .NET 10, .NET Aspire, Vue/Vite, TypeScript, and .NET/UI test projects.
- **Created:** 2026-10-18T23:22:18.977-07:00

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### Frontend skill-up (2026-10-25)

**Location & tooling** — `SimplyBudgetWeb.Web\` (`package.json`, pnpm@11 via `packageManager`; `package-lock.json` also present but CI/csproj use `pnpm install --frozen-lockfile`).
- Scripts: `pnpm dev` (vite), `pnpm build` (`vue-tsc -b && vite build`), `pnpm lint` (`eslint .`), `pnpm test` (`vitest run`), `pnpm preview`.
- Stack: Vue 3.5, vue-router 5, Pinia 4, Vuetify 4 (+ `@mdi/font` icons, `vite-plugin-vuetify` autoImport), Vite 8 (rolldown), TS ~6.0 strict (`noUnusedLocals/Parameters`), vite-plugin-pwa (autoUpdate, devOptions enabled → `dev-dist\` is generated, gitignored).
- Runtime deps: `@azure/msal-browser`, `@microsoft/signalr`, `@microsoft/applicationinsights-web`, `workbox-window`.
- Alias `@` → `src` (vite.config.ts, vitest.config.ts, tsconfig.app.json).
- ESLint flat config (`eslint.config.js`): js recommended + typescript-eslint recommended + `vue flat/essential`; `vue/multi-word-component-names` off. No Prettier.
- CI (`.github\workflows\build-and-deploy.yml`): Node 22.x; runs `pnpm run lint`, `pnpm run test`, `pnpm run build`; deploys to Azure Static Web Apps (`public\staticwebapp.config.json` has SPA navigationFallback + security headers).

**Structure** (`src\`): `pages\` (Budget, History, PendingExpenses, Receipts, Settings[~53KB, largest], Import, Accounts, Login), `components\` (dialogs: AddTransactionDialog, ConvertPendingExpenseDialog, AddRuleDialog; AmountField, CategorySelector, MonthPickerNav, IncomeAllocationList, ReceiptPreview, ReceiptImageAdjustment, SnackbarHost, Layout), `composables\useMonthQueryParam.ts`, `services\`, `stores\` (auth, snackbar), `types\index.ts` (DTOs), `utils\` (currency, receiptImage, search, externalLinks).

**Routing** (`src\router\index.ts`): `createWebHistory`; pages imported eagerly (intentional, avoids chunk round-trip). `/login` standalone; everything else is a child of `Layout.vue`: `/budget` (default), `/history` (nav label "Expenses"), `/settings` (`/accounts` redirects here), `/import`, `/pending-expenses`, `/receipts`; catch-all → `/budget`. `beforeEach` awaits `authStore.initialize()` and redirects anonymous users to `login`.

**Auth** (`src\authConfig.ts`, `src\stores\auth.ts`): MSAL redirect flow, `sessionStorage` cache; API scope `api://{clientId}/access_as_user`. `getToken()` = acquireTokenSilent → one forceRefresh retry → `handleSessionExpired()` (clears cache, warning snackbar, `navigateToLogin()` via `services\sessionNavigation.ts` to avoid router import cycle). Display name from `GET /api/current-user`.

**API access** (`src\services\apiClient.ts`): singleton `apiClient` wrapping `fetch` with `get/post/put/delete/upload(FormData)/download(blob+filename)`. Base URL = build-time `__API_BASE_URL__` (vite `define`). Adds Bearer token via `setTokenProvider`; 401 → `SessionExpiredError` + unauthorized handler. Errors surface ProblemDetails `detail`/`message`/`title` as `Error.message`. JSON bodies escape `?` as `\u003F`. `delete(url, { ifMatch })` sends `If-Match` (pending expenses use `item.version`). Endpoints are all `/api/...` string literals in pages/components — no generated client.
- Realtime: `services\monthUpdatesHub.ts` SignalR `/hubs/budget-month-updates` (`SubscribeToMonth`/`UnsubscribeFromMonth`, event `MonthUpdated`), used by Budget.vue and History.vue.
- Telemetry: `services\telemetry.ts` App Insights only if connection string defined.

**Backend URL / Aspire** (`SimplyBudgetWeb.AppHost\AppHost.cs`): `AddJavaScriptApp("SimplyBudgetWeb-frontend", "../SimplyBudgetWeb.Web", "dev").WithPnpm(install: true).WithHttpEndpoint(env: "PORT")`; injects `REACTAPP_BACKEND_HTTP/HTTPS`, `ENTRA_CLIENT_ID`, `ENTRA_TENANT_ID` (AppHost params `entra-client-id`/`entra-tenant-id`). `vite.config.ts` resolves backend from `BACKEND_URL` → `services__SimplyBudgetWeb-backend__http(s)__0` → `REACTAPP_BACKEND_*` → `https://localhost:5001`, and proxies `/api` + `/hubs` (ws) to it. Production build sets `BACKEND_URL` from Terraform output. Vuetify docs are also an AppHost resource (`AddVuetifyDocs`).

**State** — Pinia setup-style stores only for auth + snackbar; page data is local `ref`s fetched in `onMounted`. Selected month: `useMonthQueryParam()` syncs `?month=YYYY-MM` with localStorage `SimplyBudgetWebMonth`. Theme persisted in localStorage `SimplyBudgetWebTheme` (`plugins\vuetify.ts`, toggled in `Layout.vue`).

**UX conventions**
- Feedback via `useSnackbarStore().enqueueSnackbar(msg, { variant })` (success/error/info/warning), rendered by `SnackbarHost.vue`; errors typically `e instanceof Error ? e.message : 'Failed to …'`.
- Loading: `v-progress-circular` with `aria-label`; buttons use `:loading`/`:disabled`; destructive actions go through confirm `v-dialog`s (discard / delete all / delete receipt).
- Money is integer cents: `utils\currency.ts` (`formatCents`, `dollarsToCents`, `centsToDollars`, `parseLocalDate` avoids UTC shift). `AmountField.vue` commits only on blur/Enter.
- Validation is imperative/computed (e.g. `canSubmit`, `:error` on CategorySelector) — no `v-form :rules` usage anywhere.
- Theme colors `credit`/`debit` tuned for ≥4.5:1 contrast; `medium-emphasis-opacity` raised to 0.74 for WCAG AA.
- A11y patterns: `aria-label` on icon buttons, `aria-pressed` toggle buttons, `role="status"` live text, keyboard arrow-key corner moves in ReceiptImageAdjustment. UI tests (`SimplyBudgetWeb.UITests`) use Playwright + Deque.AxeCore.Playwright + TUnit via Aspire testing.

**Receipts & pending expenses**
- `pages\Receipts.vue`: "Take a photo" (`capture="environment"`) + "Choose file" hidden inputs; JPEG/PNG/WebP only (type+extension check), 1 byte–15 MB; optional corner adjustment (`ReceiptImageAdjustment.vue`, homography in `utils\receiptImage.ts`) sends `imageGeometry` JSON with untouched `image` in FormData to `POST /api/receipts`; user overrides entered during AI analysis take priority. Pending/Categorized views; `PUT/DELETE /api/receipts/{id}`.
- `ReceiptPreview.vue` downloads `/api/receipts/{id}/image`, flattens client-side, revokes object URLs.
- `pages\PendingExpenses.vue` + `ConvertPendingExpenseDialog.vue`: assignee/notes via `PUT /api/pending-expenses/{id}`, discard with If-Match, `POST .../reapply-rules`, `POST .../{id}/convert`, receipt matching via `GET /api/receipts/matches?...` (request-session guards drop stale responses).
- Note: `vite.config.ts` has a `receipt-vision` chunk group for `@techstark/opencv-js`, but that package isn't in package.json or imported in src — stale config.

**Tests** — Vitest + jsdom + @vue/test-utils; `src\**\*.test.ts` (Receipts.test.ts, apiClient.test.ts, currency.test.ts, receiptImage.test.ts). `vitest.setup.ts` stubs ResizeObserver; vuetify inlined. Tests `vi.mock('@/services/apiClient')` and `mount` real Vuetify components.