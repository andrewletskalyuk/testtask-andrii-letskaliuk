using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TestTask.Data;
using TestTask.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

builder.Services.Configure<DbConfig>(builder.Configuration.GetSection("DbConfig"));

builder.Services.AddDbContext<TestDbContext>(options =>
{
    var dbConfig = builder.Configuration.GetSection("DbConfig").Get<DbConfig>();
    options.UseNpgsql(dbConfig.ConnectionString);
});

builder.Services.AddScoped<IMarketService, MarketService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTestDbContext(builder.Configuration);
builder.Services.AddServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
await app.MigrateDatabaseAsync<TestDbContext>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();