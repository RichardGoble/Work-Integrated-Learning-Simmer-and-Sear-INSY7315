using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
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
                or Permission.CatalogueManage ? master : sales;
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
}
