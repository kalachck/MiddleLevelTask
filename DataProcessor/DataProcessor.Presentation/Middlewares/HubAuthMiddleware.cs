namespace DataProcessor.Presentation.Middlewares;

public class HubAuthMiddleware : IMiddleware
{
    private readonly string _hubKey;
    
    public HubAuthMiddleware(IConfiguration configuration)
    {
        _hubKey = configuration["SignalR:HubKey"] ?? throw new InvalidOperationException("SignalR:HubKey is not set in configuration.");
    }
    
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Path.StartsWithSegments("/hubs"))
        {
            var hubKey = context.Request.Headers["x-hub-key"].ToString();
            if (string.IsNullOrWhiteSpace(hubKey))
                hubKey = context.Request.Query["access_token"].ToString();

            if (string.IsNullOrWhiteSpace(hubKey) || hubKey != _hubKey)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "text/plain; charset=utf-8";
                await context.Response.WriteAsync("Unauthorized: invalid or missing hub key.", context.RequestAborted);
                return;
            }
        }
        
        await next(context);
    }
}
