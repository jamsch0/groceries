using DbUp;
using Groceries.Common;
using Groceries.Data;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var env = builder.Environment;

var dataDir = builder.Configuration["data"] ?? env.ContentRootPath;

builder.Configuration
    .AddIniFile(Path.Combine(dataDir, "config.ini"), optional: true, reloadOnChange: true)
    .AddIniFile(Path.Combine(dataDir, $"config_{env.EnvironmentName}.ini"), optional: true, reloadOnChange: true);

var dbConn = builder.Configuration["Database"]!;
EnsureDatabase.For.PostgresqlDatabase(dbConn);

var dbUpgradeResult = DeployChanges.To
    .PostgresqlDatabase(dbConn)
    .JournalToPostgresqlTable("public", "__dbup_migrations")
    .WithScriptsEmbeddedInAssembly(typeof(AppDbContext).Assembly)
    .WithTransactionPerScript()
    .Build()
    .PerformUpgrade();

if (!dbUpgradeResult.Successful)
{
    return -1;
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.All;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var dataProtection = builder.Services.AddDataProtection();
if (env.IsProduction())
{
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataDir, "keys")));
}

var oauthConfig = builder.Configuration.GetSection("OAuth");
if (oauthConfig.Exists())
{
    const string authenticationScheme = "OAuth";
    builder.Services.Configure<OAuthOptions>(authenticationScheme, oauthConfig);

    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.ExternalScheme;
            options.DefaultChallengeScheme = authenticationScheme;
        })
        .AddOAuth(authenticationScheme, options =>
        {
            options.SignInScheme = IdentityConstants.ExternalScheme;
            options.CallbackPath = "/signin";

            foreach (string scope in (oauthConfig["Scopes"] ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                options.Scope.Add(scope.Trim());
            }

            // Antiforgery token generation requires authenticated identities to have claims
            // and the default OAuth authentication handler does not fetch user info.
            options.Events.OnCreatingTicket = async context =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.Authorization = new AuthenticationHeaderValue(context.TokenType ?? "Bearer", context.AccessToken);

                using var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                response.EnsureSuccessStatusCode();

                using var payload = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(context.HttpContext.RequestAborted));
                context.Identity!.AddClaims(
                    payload.RootElement.EnumerateObject()
                        .Where(property => property.Value.ValueKind is JsonValueKind.String or JsonValueKind.Number)
                        .Select(property => new Claim(property.Name, property.Value.ToString())));
            };
        })
        .AddExternalCookie();

    builder.Services.AddAuthorization();
}

builder.Services
    .AddControllersWithViews()
    .AddRazorOptions(options =>
    {
        options.ViewLocationFormats.Clear();
        options.ViewLocationFormats.Add("/{1}/{0}" + RazorViewEngine.ViewExtension);
        options.ViewLocationFormats.Add("/Common/{0}" + RazorViewEngine.ViewExtension);
    })
    .AddSessionStateTempDataProvider();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

builder.Services.AddSingleton<IActionResultExecutor<TurboStreamResult>, TurboStreamResultExecutor>();

builder.Services.AddDbContextPool<AppDbContext>(options => options
    .EnableDetailedErrors(env.IsDevelopment())
    .EnableSensitiveDataLogging(env.IsDevelopment())
    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
    .UseSnakeCaseNamingConvention()
    .UseNpgsql(dbConn));

var app = builder.Build();

app.UseForwardedHeaders();
app.UseStaticFiles();
app.UseRouting();

if (oauthConfig.Exists())
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.UseSession();

var controllers = app.MapControllers();
if (oauthConfig.Exists())
{
    controllers.RequireAuthorization();
}

await app.RunAsync();

return 0;
