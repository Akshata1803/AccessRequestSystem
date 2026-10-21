using AccessRequestSystem.Data;
using AccessRequestSystem.Worker;
using AccessRequestSystem.Worker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Configure Host with Windows Service support
var host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "AccessRequestWorker";
    })
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        // Register IDbContextFactory for thread-safe DbContext creation across parallel tasks
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Domain service for revoking expired grants
        services.AddSingleton<IExpiredGrantRevoker, ExpiredGrantRevoker>();

        // Background service worker
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
