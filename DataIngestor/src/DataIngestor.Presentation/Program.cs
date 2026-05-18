using DataIngestor.Application;
using DataIngestor.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationDependencies();
builder.Services.AddInfrastructureDependencies(builder.Configuration);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("docker"))
{
    app.MapOpenApi();
}

if (!app.Environment.IsEnvironment("docker"))
{
    app.UseHttpsRedirection();
}

app.Run();
