using API.Controllers;
using Application;
using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Tests;

// Runs the real controllers, JSON binding, MediatR handlers and EF mappings.
// Never loads Program/appsettings or connects to SQL Server.
internal sealed class TestApi : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly IHost _host;

    public HttpClient Client { get; }

    public TestApi()
    {
        _connection.Open();
        _host = new HostBuilder().ConfigureWebHost(web => web.UseTestServer()
            .ConfigureServices(services =>
            {
                services.AddControllers().AddApplicationPart(typeof(ScanningController).Assembly);
                services.AddApplication();
                services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
                services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapControllers());
            })).Start();

        Client = _host.GetTestClient();
        using var scope = _host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated(); // Only this isolated SQLite in-memory database.
        context.Lines.AddRange(
            new Lines { Id = 4, LineName = "Línea 4 Empaques" },
            new Lines { Id = 8, LineName = "MicroChannel" },
            new Lines { Id = 9, LineName = "Inactiva", IsActive = false });
        context.Clients.Add(new Client { Id = 1, ClientName = "Test" });
        context.SaveChanges();
    }

    public async Task WithDb(Func<ApplicationDbContext, Task> action)
    {
        using var scope = _host.Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }

    public Task AddPart(string code, int lineId, bool active = true) => WithDb(async db =>
    {
        db.PartNumbers.Add(new PartNumber
        {
            PartNumbersCode = code, IdLine = lineId, IdClient = 1, IsActive = active
        });
        await db.SaveChangesAsync();
    });

    public void Dispose()
    {
        Client.Dispose();
        _host.Dispose();
        _connection.Dispose();
    }
}
