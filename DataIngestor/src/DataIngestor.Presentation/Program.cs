using DataIngestor.Application;
using DataIngestor.Infrastructure;
using DataIngestor.Presentation.Middlewares;
using DataIngestor.Presentation.Observability;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting Data Ingestor");

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddApplicationDependencies(builder.Configuration);
builder.Services.AddInfrastructureDependencies(builder.Configuration);

builder.Services.AddTransient<GlobalExceptionHandlerMiddleware>();

builder.Services.AddOpenApi();
builder.Services.AddObservability("data-ingestor");

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("docker"))
{
    app.MapOpenApi();
}

if (!app.Environment.IsEnvironment("docker"))
{
    app.UseHttpsRedirection();
}

app.UseSerilogRequestLogging();

app.UseObservability();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

Log.Information("Data Ingestor listening on {Urls}", string.Join(", ", app.Urls));

app.Run();
