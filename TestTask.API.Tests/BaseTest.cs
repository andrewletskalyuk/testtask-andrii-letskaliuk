using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RAIT.Core;
using TestTask.Data;
using ServiceCollectionExtensions = RAIT.Core.ServiceCollectionExtensions;

namespace TestTask.API.Tests;

public class BaseTest
{
    protected RaitHttpClientWrapper<T> Rait<T>() where T : ControllerBase
    {
        using var scope = Context.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        return Context.Client.Rait<T>();
    }

    [SetUp]
    public async Task Setup()
    {
        var application = new WebApplicationFactory<Program>().WithWebHostBuilder(PrepareEnv);
        application.Services.ConfigureRait();
        Context.Client = application.CreateDefaultClient();

        var scope = application.Services.CreateScope();
        Context.Services = scope.ServiceProvider;
        Context.DbContext = Context.Services.GetRequiredService<TestDbContext>();

        await Context.DbContext.Database.EnsureDeletedAsync();
        await Context.DbContext.Database.MigrateAsync();

        await SetupBase();
    }

    protected virtual async Task SetupBase()
    {
    }

    private static void PrepareEnv(IWebHostBuilder _)
    {
        _.UseEnvironment("Development");
        _.ConfigureTestServices(services =>
        {
            ServiceCollectionExtensions.AddRait(services);

            services.AddDbContext<TestDbContext>((serviceProvider, options) =>
            {
                var dbConfig = serviceProvider.GetRequiredService<DbConfig>();
                options.UseNpgsql(dbConfig.ConnectionString);
            });
        });
    }
}