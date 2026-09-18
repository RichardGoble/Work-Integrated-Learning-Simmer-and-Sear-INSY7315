using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SimmerAndSear.Application.Accounts;

namespace SimmerAndSear.Web.Security;

public static class IdentityRegistration
{
    public static IServiceCollection AddAccountIdentity(this IServiceCollection services)
    {
        services.AddScoped<AccountService>();
        services.AddSingleton<LoginTiming>();
        services.AddScoped<AccountCookies>();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
        {
            options.Cookie.Name = "__Host-SimmerAndSear";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.LoginPath = "/Auth/Login";
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            options.SlidingExpiration = false;
            options.EventsType = typeof(AccountCookies);
        });
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "__Host-SimmerAndSear-CSRF";
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });
        services.Configure<MvcOptions>(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
        return services;
    }
}
