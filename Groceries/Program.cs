using Groceries.Common;
using Groceries.Data;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddIniFile("config.ini", optional: true, reloadOnChange: true)
    .AddIniFile($"config_{builder.Environment.EnvironmentName}.ini", optional: true, reloadOnChange: true);

var mvc = builder.Services
    .AddControllersWithViews()
    .AddRazorOptions(options =>
    {
        options.ViewLocationFormats.Clear();
        options.ViewLocationFormats.Add("/{1}/{0}" + RazorViewEngine.ViewExtension);
        options.ViewLocationFormats.Add("/Common/{0}" + RazorViewEngine.ViewExtension);
    })
    .AddSessionStateTempDataProvider();

if (builder.Environment.IsDevelopment())
{
    mvc.AddRazorRuntimeCompilation();
}

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

builder.Services.AddSingleton<IActionResultExecutor<TurboStreamResult>, TurboStreamResultExecutor>();

builder.Services.AddDbContextPool<AppDbContext>(options => options
    .EnableDetailedErrors(builder.Environment.IsDevelopment())
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
    .UseSnakeCaseNamingConvention()
    .UseNpgsql(builder.Configuration["Database"]!));

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllers();

app.Run();
