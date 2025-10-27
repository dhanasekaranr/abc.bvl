using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentValidation;

namespace abc.bvl.AdminTool.Api.Middleware;

/// <summary>
/// Global exception handling middleware that prevents information disclosure
/// Compliant with OWASP security guidelines
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.TraceIdentifier;
        
        try
        {
            // Log the full exception details (but sanitize sensitive data)
            var sanitizedException = SanitizeException(exception);
            _logger.LogError(sanitizedException, 
                "Unhandled exception occurred. CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}, UserId: {UserId}",
                correlationId,
                context.Request.Path,
                context.Request.Method,
                GetUserId(context));
        }
        catch (Exception logEx)
        {
            // If logging fails, at least try to log that logging failed
            try
            {
                _logger.LogError(logEx, "Failed to log original exception in GlobalExceptionMiddleware");
            }
            catch
            {
                // If even that fails, we can't do much more
            }
        }

        // Check if response has already been started
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Cannot write error response, response has already started. CorrelationId: {CorrelationId}", correlationId);
            return;
        }

        var response = context.Response;
        
        // Safely set content type only if response hasn't started
        try
        {
            if (!response.HasStarted)
            {
                response.ContentType = "application/json";
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Could not set ContentType, response may have started. CorrelationId: {CorrelationId}", correlationId);
            return;
        }

        var errorResponse = exception switch
        {
            ValidationException validationEx => new ErrorResponse
            {
                Success = false,
                Message = "Validation failed",
                CorrelationId = correlationId,
                Errors = validationEx.Errors?.Select(e => new ValidationError
                {
                    Property = e.PropertyName,
                    Message = e.ErrorMessage
                }).ToArray() ?? Array.Empty<ValidationError>(),
                StatusCode = (int)HttpStatusCode.BadRequest
            },
            
            UnauthorizedAccessException => new ErrorResponse
            {
                Success = false,
                Message = "Access denied",
                CorrelationId = correlationId,
                StatusCode = (int)HttpStatusCode.Unauthorized
            },
            
            KeyNotFoundException => new ErrorResponse
            {
                Success = false,
                Message = "Resource not found",
                CorrelationId = correlationId,
                StatusCode = (int)HttpStatusCode.NotFound
            },
            
            TimeoutException => new ErrorResponse
            {
                Success = false,
                Message = "Request timeout",
                CorrelationId = correlationId,
                StatusCode = (int)HttpStatusCode.RequestTimeout
            },
            
            ArgumentException argEx when !_environment.IsDevelopment() => new ErrorResponse
            {
                Success = false,
                Message = "Invalid request parameters",
                CorrelationId = correlationId,
                StatusCode = (int)HttpStatusCode.BadRequest
            },
            
            _ => new ErrorResponse
            {
                Success = false,
                Message = _environment.IsDevelopment() 
                    ? exception.Message 
                    : "An error occurred while processing your request",
                CorrelationId = correlationId,
                StatusCode = (int)HttpStatusCode.InternalServerError,
                // Only include stack trace in development
                Details = _environment.IsDevelopment() ? exception.StackTrace : null
            }
        };

        try
        {
            response.StatusCode = errorResponse.StatusCode;
            
            var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            await response.WriteAsync(jsonResponse);
        }
        catch (Exception responseEx)
        {
            // If we can't write the response, log it and try to write a simple error
            _logger.LogError(responseEx, "Failed to write error response. CorrelationId: {CorrelationId}", correlationId);
            
            try
            {
                if (!response.HasStarted)
                {
                    response.StatusCode = 500;
                    await response.WriteAsync("An error occurred while processing your request.");
                }
            }
            catch
            {
                // Last resort - nothing more we can do
            }
        }
    }

    private static Exception SanitizeException(Exception exception)
    {
        try
        {
            // Remove sensitive information from exception messages
            var message = exception.Message;
            
            if (!string.IsNullOrEmpty(message))
            {
                // Remove potential connection strings, passwords, tokens
                message = Regex.Replace(message, 
                    @"password=[\w\d]*", "password=***", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
                message = Regex.Replace(message, 
                    @"token=[\w\d]*", "token=***", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
                message = Regex.Replace(message, 
                    @"key=[\w\d]*", "key=***", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
            }
                
            return new Exception(message, exception.InnerException);
        }
        catch
        {
            // If sanitization fails, return original exception
            return exception;
        }
    }

    private static string GetUserId(HttpContext context)
    {
        try
        {
            return context?.User?.Identity?.Name ?? "anonymous";
        }
        catch
        {
            return "anonymous";
        }
    }
}

/// <summary>
/// Standardized error response model
/// </summary>
public class ErrorResponse
{
    public bool Success { get; set; } = false;
    public string Message { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public ValidationError[] Errors { get; set; } = Array.Empty<ValidationError>();
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Validation error details
/// </summary>
public class ValidationError
{
    public string Property { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}