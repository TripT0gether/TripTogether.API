using System.Text.Json;

namespace TripTogether.API.Middlewares
{
    // Catch all unhandled exceptions in the API
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Try to run the Controller
                await _next(context);
            }
            catch (Exception ex)
            {
                // If ANY error happens in Service or Controller -> catch here
                _logger.LogError(ex, "An unhandled exception occurred.");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            // Determine Status Code based on Exception Type
            var statusCode = exception switch
            {
                KeyNotFoundException => StatusCodes.Status404NotFound,
                ArgumentException => StatusCodes.Status400BadRequest,
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                _ => StatusCodes.Status500InternalServerError
            };

            context.Response.StatusCode = statusCode;

            var response = ApiResult<object>.Failure(
                statusCode.ToString(),
                exception.Message // Or a generic message if 500
            );

            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
