using Microsoft.AspNetCore.Http;

namespace AirSecurityDroneControl.BuildingBlocks.Observability;

public sealed class BasicMetricsMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, BasicMetrics metrics)
    {
        await next(context);

        var route = context.Request.Path.HasValue ? context.Request.Path.Value! : "/";
        metrics.Record(route, context.Response.StatusCode);
    }
}
