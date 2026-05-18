using DataProcessor.Application;
using DataProcessor.Application.Interfaces.Services;
using DataProcessor.Infrastructure;
using DataProcessor.Presentation.Hubs;
using DataProcessor.Presentation.Middlewares;
using DataProcessor.Presentation.SignalR.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("UiDev", policy =>
        policy
            .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddTransient<HubAuthMiddleware>();
builder.Services.AddScoped<ISensorNotificationService, SignalRNotificationService>();

builder.Services.AddApplicationDependencies();
builder.Services.AddInfrastructureDependencies(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("docker"))
{
    app.MapOpenApi();
}

app.UseCors("UiDev");

app.UseMiddleware<HubAuthMiddleware>();

app.MapHub<SensorsHub>("/hubs/sensors");

app.Services.AddInfrastructureAppDependencies();

app.Run();
