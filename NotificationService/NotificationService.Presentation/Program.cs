using NotificationService.Application.Interfaces;
using NotificationService.Infrastructure;
using NotificationService.Presentation.Middlewares;
using NotificationService.Presentation.Observability;
using NotificationService.Presentation.SignalR.Hubs;
using NotificationService.Presentation.SignalR.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting Notification Service");

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddOpenApi();
builder.Services.AddObservability("notification-service");
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Ui", policy =>
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://localhost:3000",
                "http://127.0.0.1:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services.AddTransient<HubAuthMiddleware>();
builder.Services.AddScoped<ISensorHubBroadcaster, SignalRHubBroadcaster>();

builder.Services.AddInfrastructureDependencies(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("docker"))
{
    app.MapOpenApi();
}

app.UseCors("Ui");
app.UseSerilogRequestLogging();
app.UseObservability();
app.UseMiddleware<HubAuthMiddleware>();
app.MapHub<SensorsHub>("/hubs/sensors");

await app.Services.InitializeRabbitMqAsync();

Log.Information("Notification Service listening on {Urls}", string.Join(", ", app.Urls));

app.Run();
