using DbUp;
using Groceries.Common;
using Groceries.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;

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

var dataProtection = builder.Services.AddDataProtection();
if (env.IsProduction())
{
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataDir, "keys")));
}

var mvc = builder.Services
    .AddControllersWithViews()
    .AddRazorOptions(options =>
    {
        options.ViewLocationFormats.Clear();
        options.ViewLocationFormats.Add("/{1}/{0}" + RazorViewEngine.ViewExtension);
        options.ViewLocationFormats.Add("/Common/{0}" + RazorViewEngine.ViewExtension);
    })
    .AddSessionStateTempDataProvider();

if (env.IsDevelopment())
{
    mvc.AddRazorRuntimeCompilation();
}

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

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllers();

app.Run();

return 0;
