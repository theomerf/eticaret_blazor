using Application.Repositories.Interfaces;
using Domain.Exceptions;
using Serilog;
using Serilog.Context;
using System.Net;
using System.Text.Json;

namespace ETicaret.Middlewares
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly bool _isDevelopment;

        public GlobalExceptionHandlerMiddleware(RequestDelegate next, bool isDevelopment)
        {
            _next = next;
            _isDevelopment = isDevelopment;
        }

        public async Task InvokeAsync(HttpContext context, IRepositoryManager manager)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException)
            {
                // İstek iptal edildiğinde loglama yapmadan sessizce geç
            }
            catch (ObjectDisposedException)
            {
                // Nesne dispose edildiğinde sessizce geç
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex, _isDevelopment);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception, bool isDevelopment)
        {
            var exceptionType = exception.GetType().Name;
            var message = exception.Message;

            var severity = exception switch
            {
                UnauthorizedAccessException => "Warning",
                ArgumentException => "Warning",
                InvalidOperationException => "Error",
                _ => "Critical"
            };

            using (LogContext.PushProperty("UserId", context.User?.Identity?.Name))
            using (LogContext.PushProperty("IpAddress", context.Connection.RemoteIpAddress?.ToString()))
            using (LogContext.PushProperty("RequestPath", context.Request.Path.Value))
            using (LogContext.PushProperty("RequestMethod", context.Request.Method))
            using (LogContext.PushProperty("Severity", severity))
            {
                switch (severity)
                {
                    case "Warning":
                        Log.Warning(exception,
                            "Unhandled exception occurred. Type: {ExceptionType}, Path: {Path}, Method: {Method}",
                            exceptionType, context.Request.Path, context.Request.Method);
                        break;

                    case "Error":
                        Log.Error(exception,
                            "Unhandled exception occurred. Type: {ExceptionType}, Path: {Path}, Method: {Method}",
                            exceptionType, context.Request.Path, context.Request.Method);
                        break;

                    default:
                        Log.Fatal(exception,
                            "Unhandled exception occurred. Type: {ExceptionType}, Path: {Path}, Method: {Method}",
                            exceptionType, context.Request.Path, context.Request.Method);
                        break;
                }
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = exception switch
            {
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                ArgumentException => (int)HttpStatusCode.BadRequest,
                KeyNotFoundException => (int)HttpStatusCode.NotFound,
                NotFoundException => (int)HttpStatusCode.NotFound,
                _ => (int)HttpStatusCode.InternalServerError
            };
            var response = new
            {
                StatusCode = context.Response.StatusCode,
                Message = "İsteğiniz işlenirken bir hata oluştu.",
                Detailed = isDevelopment ? message : null
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }

    public static class GlobalExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder, bool isDevelopment)
        {
            return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>(isDevelopment);
        }
    }
}