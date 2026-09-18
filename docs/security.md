# Accounts and security

Use HTTPS locally (`dotnet run --project src/SimmerAndSear.Web --launch-profile https`). Both authentication and antiforgery cookies require HTTPS.

## Accounts

The browser forms are at `/Auth/Register` and `/Auth/Login`. `/Admin/Login` redirects to the same sign-in flow. Registration always creates an active Customer; submitted role fields cannot grant staff access. Passwords must contain 12-128 characters and are hashed with ASP.NET Identity's password hasher.

Five failed passwords lock an account for 15 minutes. MongoDB updates the counter and lockout together, including concurrent attempts. A successful login resets the counter. Cookies expire after 30 minutes without sliding renewal. Each request rechecks account activity, lockout, role, and a fingerprint of the stored password hash. Changing the role or password invalidates existing cookies. Logout clears the current browser cookie; it does not revoke copied cookies on other devices.

The JSON endpoints are:

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/api/accounts/csrf` | Obtain an antiforgery request token |
| POST | `/api/accounts/register` | Register a customer |
| POST | `/api/accounts/login` | Sign in |
| POST | `/api/accounts/logout` | Sign out |
| GET | `/api/accounts/me` | Read the current identity |
| GET | `/api/admin/audit` | Read audit history as Master Admin |

Keep the cookies returned by the CSRF endpoint and send its token in `X-CSRF-TOKEN` on every POST, including login. Fetch a fresh token after signing in or out. Razor forms generate their own tokens. Login uses `email` and `password`; registration also requires `firstName` and `lastName`.

## Permissions

| Access | Customer | Sales Admin | Master Admin |
| --- | --- | --- | --- |
| Profile | Yes | Yes | Yes |
| Wishlist and checkout | Yes | No | No |
| Admin dashboard | No | Yes | Yes |
| CatalogueManage, InquiryManage, SettingsManage | No | Yes | Yes |
| AccountManage, AuditRead | No | No | Yes |

The named permission policies are available for future feature endpoints. Existing catalogue, order, and account-management dashboard mockups do not implement administrative writes. The audit page at `/Admin/Audit` reads actual audit records. APIs return 401 when signed out and 403 when forbidden; browser pages redirect signed-out users to login and return 403 for insufficient permissions.

Registration, login, logout, rejected authorization, invalid antiforgery tokens, and rate-limit rejections create audit events. Blocked actions include `ACCESS_DENIED`. Passwords, cookies, request bodies, and CSRF tokens are excluded. If MongoDB audit storage is unavailable, the event and persistence failure are written to application logs; a denied request stays denied.

## Seed accounts

Configure `Mongo__ConnectionString` and `Mongo__Database`. Supply `Seed__MasterAdminPassword`, `Seed__SalesAdminPassword`, and `Seed__CustomerPassword` through environment variables or a secret store; all must be 12-128 characters. Emails can be overridden with the corresponding `Seed__...Email` settings. Run:

```powershell
$env:Seed__AccountsOnly = "true"
dotnet run --project src/SimmerAndSear.Seeder
```

The default Master Admin email is `master@simmerandsear.local`. No password is committed or generated automatically. Existing accounts are left untouched. A configured seed email with the wrong role or inactive status is reported as an error rather than silently promoted. With `Seed__AccountsOnly=true`, account setup does not depend on catalogue images. Without it, the seeder also loads the existing catalogue and requires the image filenames in `SeedData/products.json`. Those catalogue images are not currently present in the repository.

## Deployment

Configure explicit `AllowedHosts` for the deployment. Forwarded headers are accepted only from loopback and explicitly configured `Security:KnownProxies` addresses (environment variables such as `Security__KnownProxies__0`). Do not trust arbitrary client-supplied forwarded IPs. Persist and protect ASP.NET Data Protection keys; multiple instances must share keys to accept the same cookies.

Limits are 120 requests per minute per IP, with a shared 10-per-minute limit on browser/API login and registration. These limits are per process; multi-instance deployments should also enforce limits at the gateway. Account lockout is shared through MongoDB. Rejections return 429 with `Retry-After`.

Unexpected errors return safe ProblemDetails with a trace ID. HTTPS deployments receive HSTS outside Development (localhost is excluded by the framework). CSP allows scripts only from the same origin and blocks inline event handlers, frames, and plugins. Existing inline styles remain allowed to preserve the current views. Body limits are 10 MiB, with tighter form field limits. API and authenticated responses are not cached.

## Checks

`dotnet test` runs all tests and expects MongoDB on localhost:27017, or the connection configured through `Mongo__ConnectionString`. Tests use the `simmersear_it` database by default. Use a dedicated test database.

The HTTP security tests can also run without MongoDB:

```powershell
dotnet test tests/SimmerAndSear.IntegrationTests --filter FullyQualifiedName~SecurityTests
```

Framework references: [cookie authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0), [antiforgery](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0), and [rate limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0).
