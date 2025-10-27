using abc.bvl.AdminTool.Api.Configuration;
using Microsoft.Extensions.Options;

namespace abc.bvl.AdminTool.Api.Middleware;

/// <summary>
/// Middleware to add security headers that prevent common web vulnerabilities
/// Implements OWASP security header best practices
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecuritySettings _securitySettings;

    public SecurityHeadersMiddleware(RequestDelegate next, IOptions<SecuritySettings> securitySettings)
    {
        _next = next;
        _securitySettings = securitySettings.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers before processing the request
        AddSecurityHeaders(context);
        
        await _next(context);
        
        // Additional headers after processing (only if response hasn't started)
        try
        {
            if (!context.Response.HasStarted)
            {
                context.Response.Headers.Remove("Server");
                context.Response.Headers.Remove("X-Powered-By");
            }
        }
        catch (InvalidOperationException)
        {
            // If headers can't be removed, that's okay - just continue
        }
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        try
        {
            var headers = context.Response.Headers;

            // Only add headers if response hasn't started
            if (context.Response.HasStarted)
            {
                return;
            }

            // Prevent clickjacking attacks
            if (!headers.ContainsKey("X-Frame-Options"))
            {
                headers.Append("X-Frame-Options", "DENY");
            }

            // Prevent MIME-type sniffing
            if (!headers.ContainsKey("X-Content-Type-Options"))
            {
                headers.Append("X-Content-Type-Options", "nosniff");
            }

            // Enable XSS protection
            if (!headers.ContainsKey("X-XSS-Protection"))
            {
                headers.Append("X-XSS-Protection", "1; mode=block");
            }

        // Referrer policy
        if (!headers.ContainsKey("Referrer-Policy"))
        {
            headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        }

        // Permissions policy (formerly Feature-Policy)
        if (!headers.ContainsKey("Permissions-Policy"))
        {
            headers.Append("Permissions-Policy", 
                "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");
        }

        // Content Security Policy
        if (_securitySettings.EnableContentSecurityPolicy && !headers.ContainsKey("Content-Security-Policy"))
        {
            var csp = BuildContentSecurityPolicy();
            headers.Append("Content-Security-Policy", csp);
        }

        // Strict Transport Security (only for HTTPS)
        if (_securitySettings.EnableHsts && context.Request.IsHttps && !headers.ContainsKey("Strict-Transport-Security"))
        {
            headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
        }

            // Cache control for sensitive content
            if (IsSensitiveEndpoint(context.Request.Path))
            {
                headers.Append("Cache-Control", "no-store, no-cache, must-revalidate, private");
                headers.Append("Pragma", "no-cache");
                headers.Append("Expires", "0");
            }
        }
        catch (InvalidOperationException)
        {
            // If we can't add headers (response already started), that's okay
        }
    }

    private string BuildContentSecurityPolicy()
    {
        // Generate a nonce for this request
        var nonce = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("+", "-").Replace("/", "_").Replace("=", "");
        
        // CSP compliant configuration for Angular SPA
        // Removes 'unsafe-inline' and 'unsafe-eval' for better security
        var cspDirectives = new[]
        {
            "default-src 'self'",
            
            // Angular Runtime: Remove 'unsafe-eval', use 'strict-dynamic' for modern browsers
            // For development with Swagger, you may need to adjust this
            _securitySettings.IsDevelopment 
                ? "script-src 'self' 'unsafe-eval' 'unsafe-inline'" // Dev: Swagger/HMR support
                : $"script-src 'self' 'nonce-{nonce}' 'strict-dynamic'", // Prod: Secure nonce-based
            
            // Angular Styles: Modern Angular uses component styles, but may need unsafe-inline for legacy
            // Best practice: Use nonces or hashes for inline styles
            _securitySettings.IsDevelopment
                ? "style-src 'self' 'unsafe-inline'" // Dev: Allow inline for development
                : $"style-src 'self' 'nonce-{nonce}'", // Prod: Nonce-based styles
            
            // Images: Allow self, data URIs (for inline images), and HTTPS sources
            "img-src 'self' data: https:",
            
            // Fonts: Self and optionally CDN fonts (Google Fonts, etc.)
            "font-src 'self' data: https://fonts.gstatic.com",
            
            // API Connections: Angular HttpClient needs this for backend API calls
            "connect-src 'self' ws: wss:", // Include WebSocket for SignalR if needed
            
            // Media: Restrict media playback
            "media-src 'self'",
            
            // Objects/Plugins: Block Flash, Java, etc.
            "object-src 'none'",
            
            // Child frames: Block iframes (prevents clickjacking)
            "frame-src 'none'",
            
            // Workers: Allow web workers for Angular PWA features
            "worker-src 'self' blob:",
            
            // Frame ancestors: Prevent site from being framed
            "frame-ancestors 'none'",
            
            // Form actions: Only allow form submissions to same origin
            "form-action 'self'",
            
            // Base URI: Prevent base tag injection
            "base-uri 'self'",
            
            // Upgrade insecure requests: Force HTTPS
            "upgrade-insecure-requests",
            
            // Block all mixed content
            "block-all-mixed-content"
        };

        return string.Join("; ", cspDirectives);
    }

    private static bool IsSensitiveEndpoint(PathString path)
    {
        var sensitiveEndpoints = new[]
        {
            "/api/auth",
            "/api/admin",
            "/api/user"
        };

        return sensitiveEndpoints.Any(endpoint => 
            path.StartsWithSegments(endpoint, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Request size limiting middleware to prevent DoS attacks
/// </summary>
public class RequestSizeLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly long _maxRequestSize;
    private readonly ILogger<RequestSizeLimitMiddleware> _logger;

    public RequestSizeLimitMiddleware(
        RequestDelegate next, 
        IOptions<SecuritySettings> securitySettings,
        ILogger<RequestSizeLimitMiddleware> logger)
    {
        _next = next;
        _maxRequestSize = securitySettings.Value.MaxRequestBodySize;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.ContentLength > _maxRequestSize)
        {
            _logger.LogWarning(
                "Request rejected: Content length {ContentLength} exceeds maximum {MaxSize}. Path: {Path}, IP: {RemoteIP}",
                context.Request.ContentLength,
                _maxRequestSize,
                context.Request.Path,
                context.Connection.RemoteIpAddress);

            context.Response.StatusCode = 413; // Request Entity Too Large
            await context.Response.WriteAsync("Request size exceeds limit");
            return;
        }

        await _next(context);
    }
}

/// <summary>
/// Rate limiting middleware to prevent abuse
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecuritySettings _securitySettings;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly Dictionary<string, List<DateTime>> _requestCounts = new();
    private static readonly object _lock = new();

    public RateLimitingMiddleware(
        RequestDelegate next, 
        IOptions<SecuritySettings> securitySettings,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _securitySettings = securitySettings.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientIdentifier(context);
        
        if (IsRateLimited(clientId))
        {
            _logger.LogWarning(
                "Rate limit exceeded for client {ClientId}. Path: {Path}",
                clientId,
                context.Request.Path);

            context.Response.StatusCode = 429; // Too Many Requests
            context.Response.Headers.Append("Retry-After", "60");
            await context.Response.WriteAsync("Rate limit exceeded");
            return;
        }

        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Use authenticated user ID if available, otherwise IP address
        return context.User?.Identity?.Name ?? 
               context.Connection.RemoteIpAddress?.ToString() ?? 
               "unknown";
    }

    private bool IsRateLimited(string clientId)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var windowStart = now.AddMinutes(-_securitySettings.RateLimitWindowMinutes);

            if (!_requestCounts.ContainsKey(clientId))
            {
                _requestCounts[clientId] = new List<DateTime>();
            }

            var requests = _requestCounts[clientId];
            
            // Remove old requests outside the window
            requests.RemoveAll(req => req < windowStart);
            
            // Check if limit exceeded
            if (requests.Count >= _securitySettings.RateLimitRequests)
            {
                return true;
            }

            // Add current request
            requests.Add(now);
            return false;
        }
    }
}