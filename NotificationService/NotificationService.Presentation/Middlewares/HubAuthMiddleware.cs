namespace NotificationService.Presentation.Middlewares;

public class HubAuthMiddleware : IMiddleware
{
    private readonly string _hubKey;
    private readonly ILogger<HubAuthMiddleware> _logger;

    public HubAuthMiddleware(IConfiguration configuration, ILogger<HubAuthMiddleware> logger)
    {
        _hubKey = configuration["SignalR:HubKey"]
            ?? throw new InvalidOperationException("SignalR:HubKey is not set in configuration.");
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Path.StartsWithSegments("/hubs"))
        {
            var hubKey = context.Request.Headers["x-hub-key"].ToString();

            if (string.IsNullOrWhiteSpace(hubKey))
            {
                var authHeader = context.Request.Headers["Authorization"].ToString();
                if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    hubKey = authHeader["Bearer ".Length..].Trim();
                }
            }

            if (string.IsNullOrWhiteSpace(hubKey) || hubKey != _hubKey)
            {
                _logger.LogWarning(
                    "Rejected SignalR request to {Path} from {RemoteIp}: invalid or missing hub key",
                    context.Request.Path,
                    context.Connection.RemoteIpAddress);

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "text/plain; charset=utf-8";
                await context.Response.WriteAsync(
                    "Unauthorized: invalid or missing hub key.",
                    context.RequestAborted);
                return;
            }

            _logger.LogDebug(
                "Authorized SignalR request to {Path} from {RemoteIp}",
                context.Request.Path,
                context.Connection.RemoteIpAddress);
        }

        await next(context);
    }
}
