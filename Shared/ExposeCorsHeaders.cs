using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace TestStore.Shared;

// Host/gateway CORS still owns allowed origins and preflight. This only exposes
// Heimdall's safe-retry response marker, including on validation failures.
public sealed class ExposeCorsHeaders : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var http = context.GetHttpContext();
        if (http is not null)
            http.Response.Headers.AccessControlExposeHeaders = "X-Heimdall-Antiforgery";
        await next(context);
    }
}
