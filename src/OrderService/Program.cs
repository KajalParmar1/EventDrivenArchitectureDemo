using EdaDemo.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OrderService.Application;
using OrderService.Data;
using OrderService.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, logger) =>
    logger.ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Service", "OrderService"));

builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection(KafkaOptions.SectionName));
builder.Services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
builder.Services.AddScoped<IIdempotencyStore, EfIdempotencyStore<OrderDbContext>>();
builder.Services.AddScoped<IOrderApplicationService, OrderApplicationService>();
builder.Services.AddScoped<OutboxProcessor>();
builder.Services.AddHostedService<OutboxPublisherWorker>();

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrderDbContext>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Order Service",
        Version = "v1",
        Description = "Entry point for the Event-Driven Architecture demo. Creating an order publishes OrderCreated via the transactional outbox."
    });

    var xml = Path.Combine(AppContext.BaseDirectory, "OrderService.xml");
    if (File.Exists(xml))
    {
        options.IncludeXmlComments(xml);
    }
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    await db.Database.MigrateAsync();
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Service v1");
    options.RoutePrefix = "swagger";
});

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

public partial class Program;
