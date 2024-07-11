using DbUp;
using Groceries.Data;
using Microsoft.AspNetCore.DataProtection;
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

builder.Services
    .AddControllers()
    .AddSessionStateTempDataProvider();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession();

builder.Services.AddDbContextPool<AppDbContext>(options => options
    .EnableDetailedErrors(env.IsDevelopment())
    .EnableSensitiveDataLogging(env.IsDevelopment())
    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
    .UseSnakeCaseNamingConvention()
    .UseNpgsql(dbConn));

var app = builder.Build();

app.UseRouting();
app.UseSession();

app.MapStaticAssets();
app.MapControllers()
    .WithStaticAssets();

await app.RunAsync();

return 0;
