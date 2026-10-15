using EdaDemo.Messaging;
using Microsoft.EntityFrameworkCore;
using NotificationService;
using NotificationService.Data;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, logger) =>
    logger.ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Service", "NotificationService"));

builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection(KafkaOptions.SectionName));

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IIdempotencyStore, EfIdempotencyStore<NotificationDbContext>>();
builder.Services.AddEdaMessaging<NotificationMessageHandler>();
builder.Services.AddHealthChecks();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapHealthChecks("/health");
app.Run();
