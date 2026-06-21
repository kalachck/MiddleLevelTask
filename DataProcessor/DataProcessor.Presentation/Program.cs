using DataProcessor.Application;
using DataProcessor.Infrastructure;
using DataProcessor.Presentation.Middlewares;
using DataProcessor.Presentation.Observability;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting Data Processor");

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddOpenApi();
builder.Services.AddObservability("data-processor");

builder.Services.AddTransient<GlobalExceptionHandlerMiddleware>();

builder.Services.AddApplicationDependencies();
builder.Services.AddInfrastructureDependencies(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("docker"))
{
    app.MapOpenApi();
}

app.UseSerilogRequestLogging();

app.UseObservability();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.Services.AddInfrastructureAppDependencies();

Log.Information("Data Processor listening on {Urls}", string.Join(", ", app.Urls));

app.Run();
