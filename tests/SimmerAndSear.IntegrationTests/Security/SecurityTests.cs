using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;
using SimmerAndSear.Web.Security;

namespace SimmerAndSear.IntegrationTests.Security;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Integration tests in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//Microsoft, 2026. Use cookie authentication without ASP.NET Core Identity [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//Microsoft, 2026. Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class SecurityTests
{
    public const string Password = "correct horse battery staple";

    private static async Task<string> Token(HttpClient client)
    {
        var response = await client.GetFromJsonAsync<JsonElement>("/api/accounts/csrf");
        return response.GetProperty("token").GetString()!;
    }

    private static async Task<HttpResponseMessage> Post(HttpClient client, string url, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", await Token(client));
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task Registration_only_creates_customers_and_hashes_passwords()
    {
        using var factory = new SecurityFactory();
        using var client = factory.Browser();
        var response = await Post(client, "/api/accounts/register", new
        { email = "New@Example.com", password = Password, firstName = "New", lastName = "User", role = "MasterAdmin", isActive = false });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var user = factory.Users.Items.Single();
        user.Role.ShouldBe(RoleName.Customer);
        user.IsActive.ShouldBeTrue();
        user.Email.ShouldBe("new@example.com");
        user.PasswordHash.ShouldNotBe(Password);
        factory.Audit.Items.ShouldContain(a => a.Action == AuditAction.Create);
        (await client.GetAsync("/api/accounts/me")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_sets_secure_cookie_and_logout_clears_access()
    {
        using var factory = new SecurityFactory();
        using var client = factory.Browser();
        var user = factory.AddUser();
        var response = await Post(client, "/api/accounts/login", new { user.Email, password = Password });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("__Host-SimmerAndSear="));
        cookie.ShouldContain("secure");
        cookie.ShouldContain("httponly");
        cookie.ShouldContain("samesite=lax");
        (await client.GetAsync("/api/accounts/me")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await Post(client, "/api/accounts/logout", new { })).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await client.GetAsync("/api/accounts/me")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        factory.Audit.Items.ShouldContain(a => a.Action == AuditAction.Login);
        factory.Audit.Items.ShouldContain(a => a.Action == AuditAction.Logout);
    }

    [Theory]
    [InlineData(RoleName.Customer, 403, 200)]
    [InlineData(RoleName.SalesAdmin, 200, 403)]
    [InlineData(RoleName.MasterAdmin, 200, 403)]
    public async Task Roles_control_staff_and_customer_pages(RoleName role, int adminStatus, int customerStatus)
    {
        using var factory = new SecurityFactory();
        using var client = factory.Browser();
        var user = factory.AddUser(role);
        (await Post(client, "/api/accounts/login", new { user.Email, password = Password })).EnsureSuccessStatusCode();
        (await client.GetAsync("/Admin")).StatusCode.ShouldBe((HttpStatusCode)adminStatus);
        (await client.GetAsync("/Wishlist")).StatusCode.ShouldBe((HttpStatusCode)customerStatus);
        factory.Audit.Items.ShouldContain(a => a.Action == AuditAction.AccessDenied && a.Summary.Contains("ACCESS_DENIED"));
    }

    [Theory]
    [InlineData(RoleName.Customer, 403)]
    [InlineData(RoleName.SalesAdmin, 403)]
    [InlineData(RoleName.MasterAdmin, 200)]
    public async Task Only_master_admin_can_read_audit(RoleName role, int expected)
    {
        using var factory = new SecurityFactory();
        using var client = factory.Browser();
        var user = factory.AddUser(role);
        (await Post(client, "/api/accounts/login", new { user.Email, password = Password })).EnsureSuccessStatusCode();
        (await client.GetAsync("/api/admin/audit")).StatusCode.ShouldBe((HttpStatusCode)expected);
        (await client.GetAsync("/Admin/Audit")).StatusCode.ShouldBe((HttpStatusCode)expected);
        var dashboard = await client.GetStringAsync("/Auth/Profile");
        dashboard.ShouldContain("__RequestVerificationToken");
    }

    [Fact]
    public async Task Anonymous_pages_redirect_but_apis_return_401()
    {
        using var factory = new SecurityFactory();
        using var client = factory.Browser();
        var page = await client.GetAsync("/Admin");
        page.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        page.Headers.Location!.OriginalString.ShouldContain("/Auth/Login");
        var api = await client.GetAsync("/api/admin/audit");
        api.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        api.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        factory.Audit.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Failed_passwords_lock_account_and_expiry_allows_login()
    {
        using var factory = new SecurityFactory();
        using var client = factory.Browser();
        var user = factory.AddUser();
        for (var i = 0; i < 5; i++)
            (await Post(client, "/api/accounts/login", new { user.Email, password = "incorrect" })).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        user.IsLockedOut(DateTime.UtcNow).ShouldBeTrue();
        (await Post(client, "/api/accounts/login", new { user.Email, password = Password })).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        user.LockedOutUntil = DateTime.UtcNow.AddMinutes(-1);
        (await Post(client, "/api/accounts/login", new { user.Email, password = Password })).StatusCode.ShouldBe(HttpStatusCode.OK);
        user.FailedLoginAttempts.ShouldBe(0);
    }

    [Theory]
    [InlineData("disabled")]
    [InlineData("role")]
    [InlineData("password")]
    [InlineData("locked")]
    public async Task Existing_cookie_is_rejected_when_account_changes(string change)
    {
        using var factory = new SecurityFactory();
        using var client = factory.Browser();
        var user = factory.AddUser();
        (await Post(client, "/api/accounts/login", new { user.Email, password = Password })).EnsureSuccessStatusCode();
        switch (change)
        {
            case "disabled": user.IsActive = false; break;
            case "role": user.Role = RoleName.MasterAdmin; break;
            case "password": user.PasswordHash = "changed"; break;
            case "locked": user.LockedOutUntil = DateTime.UtcNow.AddMinutes(15); break;
        }
        (await client.GetAsync("/api/accounts/me")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/api/accounts/login")]
    [InlineData("/api/accounts/register")]
    public async Task Missing_csrf_token_is_blocked_and_audited(string path)
    {
        using var factory = new SecurityFactory();
        using var client = factory.Browser();
        var response = await client.PostAsJsonAsync(path, new { email = "test@example.com", password = Password });
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("security token");
        factory.Audit.Items.ShouldContain(a => a.Action == AuditAction.AccessDenied);
        factory.Users.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Validation_rejects_bad_email_and_short_password()
    {
        using var factory = new SecurityFactory();
        using var client = factory.Browser();
        var response = await Post(client, "/api/accounts/register", new
            { email = "invalid", password = "short", firstName = " ", lastName = "User" });
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        factory.Users.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Rate_limits_cannot_be_bypassed_with_forwarded_headers()
    {
        using var factory = new SecurityFactory();
        using var client = factory.Browser();
        for (var i = 0; i < 10; i++)
        {
            client.DefaultRequestHeaders.Remove("X-Forwarded-For");
            client.DefaultRequestHeaders.Add("X-Forwarded-For", $"203.0.113.{i + 1}");
            (await Post(client, "/api/accounts/login", new { email = "absent@example.com", password = Password })).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
        var response = await Post(client, "/api/accounts/register", new { });
        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter.ShouldNotBeNull();
    }

    [Fact]
    public async Task Exceptions_are_safe_and_security_headers_are_present()
    {
        using var factory = new SecurityFactory();
        using var client = factory.Browser();
        factory.Users.ThrowOnRead = true;
        var response = await Post(client, "/api/accounts/login", new { email = "test@example.com", password = Password });
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldNotContain("private database credentials");
        body.ShouldNotContain("StackTrace");
        body.ShouldContain("traceId");
        response.Headers.GetValues("Content-Security-Policy").Single().ShouldContain("script-src-attr 'none'");
        response.Headers.GetValues("X-Content-Type-Options").Single().ShouldBe("nosniff");
        response.Headers.Contains("Strict-Transport-Security").ShouldBeFalse();
    }

    [Fact]
    public async Task Audit_failure_does_not_turn_denial_into_access()
    {
        using var factory = new SecurityFactory();
        using var client = factory.Browser();
        factory.Audit.ThrowOnWrite = true;
        (await client.GetAsync("/api/admin/audit")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Browser_form_login_uses_csrf_and_rejects_external_return_url()
    {
        using var factory = new SecurityFactory();
        using var client = factory.Browser();
        var user = factory.AddUser();
        var html = await client.GetStringAsync("/Auth/Login");
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        token.ShouldNotBeNullOrEmpty();

        var response = await client.PostAsync("/Auth/Login?returnUrl=https%3A%2F%2Fevil.example", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["Email"] = user.Email, ["Password"] = Password, ["__RequestVerificationToken"] = token }));
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);


        response.Headers.Location!.OriginalString.ShouldBe("/");
        var profile = await client.GetStringAsync("/Auth/Profile");
        profile.ShouldContain(user.Email);
        profile.ShouldNotContain(Password);
    }

    [Theory]
    [InlineData(RoleName.Customer, false, false)]
    [InlineData(RoleName.SalesAdmin, true, false)]
    [InlineData(RoleName.MasterAdmin, true, true)]
    public async Task Permission_policies_enforce_role_matrix(RoleName role, bool sales, bool master)
    {
        using var factory = new SecurityFactory();
        using var scope = factory.Services.CreateScope();
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var principal = AccountCookies.CreatePrincipal(factory.AddUser(role));
        foreach (var permission in Enum.GetValues<Permission>())
        {
           
            var expected = permission is Permission.AccountManage or Permission.AuditRead
                or Permission.CatalogueManage or Permission.SettingsManage ? master : sales;
            (await authorization.AuthorizeAsync(principal, null, permission.ToString())).Succeeded.ShouldBe(expected);
        }
    }

    [Fact]
    public async Task Production_https_responses_include_hsts()
    {
        using var factory = new SecurityFactory();
        using var client = factory.Browser();
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://shop.example.com/health/live");
        var response = await client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("Strict-Transport-Security").Single().ShouldContain("max-age=31536000");
    }

    [Fact]
    public async Task Logout_requires_post_and_a_token_for_the_signed_in_user()
    {
        using var factory = new SecurityFactory();
        using var client = factory.Browser();
        var user = factory.AddUser();
        var anonymousToken = await Token(client);
        (await Post(client, "/api/accounts/login", new { user.Email, password = Password })).EnsureSuccessStatusCode();
        (await client.GetAsync("/api/accounts/logout")).StatusCode.ShouldBeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/accounts/logout") { Content = JsonContent.Create(new { }) };
        request.Headers.Add("X-CSRF-TOKEN", anonymousToken);
        (await client.SendAsync(request)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.GetAsync("/api/accounts/me")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Disabled_and_missing_accounts_get_the_same_login_error()
    {
        using var factory = new SecurityFactory();
        using var client = factory.Browser();
        var user = factory.AddUser();
        user.IsActive = false;
        var disabled = await Post(client, "/api/accounts/login", new { user.Email, password = Password });
        var missing = await Post(client, "/api/accounts/login", new { email = "missing@example.com", password = Password });
        disabled.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        missing.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var disabledBody = await disabled.Content.ReadFromJsonAsync<JsonElement>();
        var missingBody = await missing.Content.ReadFromJsonAsync<JsonElement>();
        disabledBody.GetProperty("title").GetString().ShouldBe(missingBody.GetProperty("title").GetString());
    }

    [Theory]
    [InlineData("/api/admin/accounts", null, 401)]
    [InlineData("/api/admin/accounts", RoleName.Customer, 403)]
    [InlineData("/api/admin/accounts", RoleName.SalesAdmin, 403)]
    [InlineData("/api/admin/accounts", RoleName.MasterAdmin, 200)]
    [InlineData("/api/orders", null, 401)]
    [InlineData("/api/orders", RoleName.SalesAdmin, 403)]
    [InlineData("/api/orders", RoleName.Customer, 200)]
    [InlineData("/api/wishlist", null, 401)]
    [InlineData("/api/wishlist", RoleName.MasterAdmin, 403)]
    [InlineData("/api/wishlist", RoleName.Customer, 200)]
    public async Task S5_endpoints_enforce_role_matrix(string path, RoleName? role, int expectedStatus)
    {
        using var factory = new SecurityFactory();
        using var client = factory.Browser();
        if (role is { } signedInRole)
        {
            var user = factory.AddUser(signedInRole);
            (await Post(client, "/api/accounts/login", new { user.Email, password = Password })).EnsureSuccessStatusCode();
        }
        (await client.GetAsync(path)).StatusCode.ShouldBe((HttpStatusCode)expectedStatus);
        if (expectedStatus == 403)
        {
            factory.Audit.Items.ShouldContain(a => a.Action == AuditAction.AccessDenied && a.Summary.Contains("ACCESS_DENIED"));
        }
    }

    [Fact]
    public async Task Customer_gets_404_for_another_customers_order()
    {
        using var factory = new SecurityFactory();
        using var clientA = factory.Browser();
        using var clientB = factory.Browser();
        var customerA = factory.AddUser();
        var customerB = factory.AddUser();
        const string orderId = "650000000000000000000001";
        factory.Orders.Items.Add(new Order { Id = orderId, ClientId = customerB.Id, OrderReference = "SS-TEST-0001" });
        (await Post(clientA, "/api/accounts/login", new { customerA.Email, password = Password })).EnsureSuccessStatusCode();
        (await Post(clientB, "/api/accounts/login", new { customerB.Email, password = Password })).EnsureSuccessStatusCode();
        (await clientA.GetAsync($"/api/orders/{orderId}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await clientB.GetAsync($"/api/orders/{orderId}")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Master_admin_creates_and_deactivates_admin_immediately()
    {
        using var factory = new SecurityFactory();
        using var master = factory.Browser();
        using var sales = factory.Browser();
        var admin = factory.AddUser(RoleName.MasterAdmin);
        (await Post(master, "/api/accounts/login", new { admin.Email, password = Password })).EnsureSuccessStatusCode();

        var created = await Post(master, "/api/admin/accounts/sales-admins", new
        {
            email = "Sales@Example.com", password = Password, firstName = "Sales", lastName = "Admin",
            role = "MasterAdmin", userType = "Client", isActive = false
        });
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var stored = factory.Users.Items.Single(u => u.Email == "sales@example.com");
        stored.Role.ShouldBe(RoleName.SalesAdmin);
        stored.UserType.ShouldBe(UserType.Admin);
        stored.IsActive.ShouldBeTrue();

        (await Post(sales, "/api/accounts/login", new { stored.Email, password = Password })).EnsureSuccessStatusCode();
        (await Post(master, $"/api/admin/accounts/{stored.Id}/deactivate", new { })).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await sales.GetAsync("/api/accounts/me")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        factory.Audit.Items.ShouldContain(a => a.Action == AuditAction.Update && a.EntityType == "User");
    }

    [Theory]
    [InlineData("POST", "/api/wishlist/items/650000000000000000000009/move-to-cart", null, 401)]
    [InlineData("POST", "/api/wishlist/items/650000000000000000000009/move-to-cart", RoleName.MasterAdmin, 403)]
    [InlineData("POST", "/api/wishlist/items/650000000000000000000009/move-to-cart", RoleName.Customer, 404)]
    [InlineData("GET", "/api/orders/650000000000000000000009/quotation", null, 401)]
    [InlineData("GET", "/api/orders/650000000000000000000009/quotation", RoleName.SalesAdmin, 403)]
    [InlineData("GET", "/api/orders/650000000000000000000009/quotation", RoleName.Customer, 404)]
    [InlineData("GET", "/api/admin/orders/650000000000000000000009/quotation", null, 401)]
    [InlineData("GET", "/api/admin/orders/650000000000000000000009/quotation", RoleName.Customer, 403)]
    [InlineData("GET", "/api/admin/orders/650000000000000000000009/quotation", RoleName.SalesAdmin, 404)]
    [InlineData("GET", "/api/admin/orders/650000000000000000000009/quotation", RoleName.MasterAdmin, 404)]
    [InlineData("POST", "/api/wishlist/quote-request", null, 401)]
    [InlineData("POST", "/api/wishlist/quote-request", RoleName.SalesAdmin, 403)]
    [InlineData("POST", "/api/wishlist/quote-request", RoleName.Customer, 400)]
    public async Task S8_endpoints_enforce_role_matrix(string method, string path, RoleName? role, int expectedStatus)
    {
        using var factory = new SecurityFactory();
        using var client = factory.Browser();
        if (role is { } signedInRole)
        {
            var user = factory.AddUser(signedInRole);
            (await Post(client, "/api/accounts/login", new { user.Email, password = Password })).EnsureSuccessStatusCode();
        }
        var response = method == "POST" ? await Post(client, path, new { }) : await client.GetAsync(path);
        response.StatusCode.ShouldBe((HttpStatusCode)expectedStatus);
        if (expectedStatus == 403)
        {
            factory.Audit.Items.ShouldContain(a => a.Action == AuditAction.AccessDenied && a.Summary.Contains("ACCESS_DENIED"));
        }
    }

    [Fact]
    public async Task Quotation_downloads_as_pdf_and_is_stored_once()
    {
        using var factory = new SecurityFactory();
        using var clientA = factory.Browser();
        using var clientB = factory.Browser();
        using var sales = factory.Browser();
        var customerA = factory.AddUser();
        var customerB = factory.AddUser();
        var salesAdmin = factory.AddUser(RoleName.SalesAdmin);
        const string quoteId = "650000000000000000000002";
        const string checkoutId = "650000000000000000000003";
        factory.Orders.Items.Add(new Order
        {
            Id = quoteId, ClientId = customerB.Id, OrderReference = "SS-TEST-0002", OrderType = OrderType.QuoteRequest,
            Items = [new OrderItem { ProductId = "650000000000000000000100", ProductName = "Cast Iron Skillet", UnitPrice = 450m, Quantity = 2 }],
            Subtotal = 900m, Total = 900m,
            ShippingDetails = new ShippingDetails { FullName = "Customer B", Email = customerB.Email }
        });
        factory.Orders.Items.Add(new Order
        {
            Id = checkoutId, ClientId = customerB.Id, OrderReference = "SS-TEST-0003", OrderType = OrderType.Checkout
        });
        (await Post(clientA, "/api/accounts/login", new { customerA.Email, password = Password })).EnsureSuccessStatusCode();
        (await Post(clientB, "/api/accounts/login", new { customerB.Email, password = Password })).EnsureSuccessStatusCode();
        (await Post(sales, "/api/accounts/login", new { salesAdmin.Email, password = Password })).EnsureSuccessStatusCode();

        var first = await clientB.GetAsync($"/api/orders/{quoteId}/quotation");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        first.Content.Headers.ContentType!.MediaType.ShouldBe("application/pdf");
        first.Content.Headers.ContentDisposition!.ToString().ShouldContain("Quotation-SS-TEST-0002.pdf");
        System.Text.Encoding.ASCII.GetString(await first.Content.ReadAsByteArrayAsync()).ShouldStartWith("%PDF");

        (await clientB.GetAsync($"/api/orders/{quoteId}/quotation")).StatusCode.ShouldBe(HttpStatusCode.OK);
        factory.Files.Items.Count.ShouldBe(1);
        factory.Quotations.Items.Count.ShouldBe(1);

        (await clientB.GetAsync($"/api/orders/{checkoutId}/quotation")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await clientA.GetAsync($"/api/orders/{quoteId}/quotation")).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var staff = await sales.GetAsync($"/api/admin/orders/{quoteId}/quotation");
        staff.StatusCode.ShouldBe(HttpStatusCode.OK);
        staff.Content.Headers.ContentType!.MediaType.ShouldBe("application/pdf");
    }

    [Fact]
    public async Task Wishlist_quote_request_raises_a_quotation_and_keeps_the_wishlist()
    {
        using var factory = new SecurityFactory();
        using var client = factory.Browser();
        var customer = factory.AddUser();
        var request = new { name = "Quote Customer", email = customer.Email, company = "Acme Kitchens", notes = "Need these by March." };

        string productId;
        using (var scope = factory.Services.CreateScope())
        {
            productId = await scope.ServiceProvider.GetRequiredService<SimmerAndSear.Application.Abstractions.IProductRepository>()
                .CreateAsync(new Product
                {
                    Name = "Quote Test Skillet", Sku = $"QT-{Guid.NewGuid():N}", Brand = "Heritage",
                    Price = 650m, StockQuantity = 5, IsPublished = true
                });
        }

        (await Post(client, "/api/accounts/login", new { customer.Email, password = Password })).EnsureSuccessStatusCode();
        (await Post(client, "/api/wishlist/quote-request", request)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        (await Post(client, "/api/wishlist/items", new { productId })).EnsureSuccessStatusCode();
        var placed = await Post(client, "/api/wishlist/quote-request", request);
        placed.StatusCode.ShouldBe(HttpStatusCode.Created);

        var order = factory.Orders.Items.ShouldHaveSingleItem();
        order.OrderType.ShouldBe(OrderType.QuoteRequest);
        order.ClientId.ShouldBe(customer.Id);
        order.CompanyName.ShouldBe("Acme Kitchens");
        order.CustomerNotes.ShouldBe("Need these by March.");
        order.Items.ShouldHaveSingleItem().Quantity.ShouldBe(1);
        order.Total.ShouldBe(650m);
        factory.Wishlists.Items.Single(w => w.ClientId == customer.Id).Items.ShouldHaveSingleItem();

        var pdf = await client.GetAsync($"/api/orders/{order.Id}/quotation");
        pdf.StatusCode.ShouldBe(HttpStatusCode.OK);
        pdf.Content.Headers.ContentType!.MediaType.ShouldBe("application/pdf");
    }

    [Theory]
    [InlineData("GET", "/api/admin/contact", null, 401)]
    [InlineData("GET", "/api/admin/contact", RoleName.Customer, 403)]
    [InlineData("GET", "/api/admin/contact", RoleName.SalesAdmin, 200)]
    [InlineData("GET", "/api/admin/contact", RoleName.MasterAdmin, 200)]
    [InlineData("GET", "/api/admin/bookings", null, 401)]
    [InlineData("GET", "/api/admin/bookings", RoleName.Customer, 403)]
    [InlineData("GET", "/api/admin/bookings", RoleName.SalesAdmin, 200)]
    [InlineData("GET", "/api/admin/bookings", RoleName.MasterAdmin, 200)]
    [InlineData("PUT", "/api/admin/contact/650000000000000000000009", null, 401)]
    [InlineData("PUT", "/api/admin/contact/650000000000000000000009", RoleName.Customer, 403)]
    [InlineData("PUT", "/api/admin/contact/650000000000000000000009", RoleName.SalesAdmin, 404)]
    [InlineData("PUT", "/api/admin/contact/650000000000000000000009", RoleName.MasterAdmin, 404)]
    public async Task S6_admin_endpoints_enforce_role_matrix(string method, string path, RoleName? role, int expectedStatus)
    {
        using var factory = new SecurityFactory();
        using var client = factory.Browser();
        if (role is { } signedInRole)
        {
            var user = factory.AddUser(signedInRole);
            (await Post(client, "/api/accounts/login", new { user.Email, password = Password })).EnsureSuccessStatusCode();
        }

        HttpResponseMessage response;
        if (method == "PUT")
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, path) { Content = JsonContent.Create(new { status = "InProgress" }) };
            request.Headers.Add("X-CSRF-TOKEN", await Token(client));
            response = await client.SendAsync(request);
        }
        else
        {
            response = await client.GetAsync(path);
        }

        response.StatusCode.ShouldBe((HttpStatusCode)expectedStatus);
        if (expectedStatus == 403)
        {
            factory.Audit.Items.ShouldContain(a => a.Action == AuditAction.AccessDenied && a.Summary.Contains("ACCESS_DENIED"));
        }
    }

    [Fact]
    public async Task S6_contact_submission_allows_anonymous_and_stores_request()
    {
        using var factory = new SecurityFactory();
        using var client = factory.Browser();

        var validContact = new
        {
            fullName = "Test Lead",
            email = "lead@example.com",
            phone = "0821234567",
            inquiryType = "ProductInquiry",
            message = "Hello, I would like more information on knives."
        };

        var response = await Post(client, "/api/contact", validContact);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        factory.Contacts.Items.ShouldHaveSingleItem();
        factory.Contacts.Items[0].FullName.ShouldBe("Test Lead");
        factory.Contacts.Items[0].NotificationStatus.ShouldBeOneOf(ContactNotificationStatus.Pending, ContactNotificationStatus.Sent);
    }

    [Fact]
    public async Task S6_booking_submission_allows_anonymous_and_authenticated_customer()
    {
        using var factory = new SecurityFactory();
        using var client = factory.Browser();

        var bookingTime = DateTime.UtcNow.AddDays(3);
        var anonBooking = new
        {
            fullName = "Anon Buyer",
            email = "buyer@example.com",
            phone = "0829876543",
            preferredDateTime = bookingTime,
            purpose = "Need consultation on cookware range."
        };

        var responseAnon = await Post(client, "/api/bookings", anonBooking);
        responseAnon.StatusCode.ShouldBe(HttpStatusCode.Created);
        factory.Bookings.Items.ShouldHaveSingleItem();

        var customer = factory.AddUser(RoleName.Customer);
        (await Post(client, "/api/accounts/login", new { customer.Email, password = Password })).EnsureSuccessStatusCode();

        var customerBooking = new
        {
            fullName = "Logged In",
            email = customer.Email,
            phone = "0825551234",
            preferredDateTime = bookingTime.AddHours(2),
            purpose = "Existing customer booking."
        };

        var responseAuth = await Post(client, "/api/bookings", customerBooking);
        responseAuth.StatusCode.ShouldBe(HttpStatusCode.Created);
        factory.Bookings.Items.Count.ShouldBe(2);
        factory.Bookings.Items.ShouldContain(b => b.ClientId == customer.Id);
    }

    [Fact]
    public async Task S6_public_reviews_endpoint_allows_anonymous_access()
    {
        using var factory = new SecurityFactory();
        using var client = factory.Browser();

        var response = await client.GetAsync("/api/products/650000000000000000000009/reviews");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
