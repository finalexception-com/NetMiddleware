using System.Collections.Concurrent;

namespace FinalException.Middleware.Example
{
    public class ThrottlingMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly ConcurrentDictionary<string, (DateTime Timestamp, int Count)> _requestCounts = new();
        private readonly TimeSpan _timeFrame = TimeSpan.FromMinutes(1);
        private readonly int _maxRequests = 5;

        public ThrottlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();

            if (ipAddress != null)
            {
                var now = DateTime.UtcNow;

                var (timestamp, count) = _requestCounts.GetOrAdd(ipAddress, (now, 0));
                if (now - timestamp > _timeFrame)
                {
                    _requestCounts[ipAddress] = (now, 1);
                }
                else
                {
                    count++;
                    if (count > _maxRequests)
                    {
                        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                        await context.Response.WriteAsync("Too many requests.");
                        return;
                    }
                    _requestCounts[ipAddress] = (timestamp, count);
                }
            }

            await _next(context);
        }
    }
}
