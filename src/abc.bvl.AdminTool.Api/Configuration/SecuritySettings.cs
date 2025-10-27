using System.ComponentModel.DataAnnotations;

namespace abc.bvl.AdminTool.Api.Configuration;

/// <summary>
/// JWT authentication settings with proper validation
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    [Required]
    [MinLength(64, ErrorMessage = "JWT secret key must be at least 64 characters for security")]
    public string SecretKey { get; set; } = string.Empty;

    [Required]
    [Url]
    public string Issuer { get; set; } = string.Empty;

    [Required]  
    [Url]
    public string Audience { get; set; } = string.Empty;

    [Range(15, 43200, ErrorMessage = "Token expiration must be between 15 minutes and 30 days")]
    public int ExpirationMinutes { get; set; } = 60;

    [Range(1, 10080, ErrorMessage = "Refresh token expiration must be between 1 and 7 days")]  
    public int RefreshTokenExpirationDays { get; set; } = 7;
}

/// <summary>
/// Security policy settings
/// </summary>
public class SecuritySettings
{
    public const string SectionName = "Security";

    public bool RequireHttps { get; set; } = true;
    public bool EnableCors { get; set; } = true;
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    public int MaxRequestBodySize { get; set; } = 10_485_760; // 10MB
    public int RateLimitRequests { get; set; } = 100;
    public int RateLimitWindowMinutes { get; set; } = 1;
    public bool EnableHsts { get; set; } = true;
    public bool EnableContentSecurityPolicy { get; set; } = true;
    public bool IsDevelopment { get; set; } = false; // Set to true in Development environment for relaxed CSP
}

/// <summary>
/// Logging settings that comply with security standards
/// </summary>
public class LoggingSettings
{
    public const string SectionName = "Logging";

    public bool EnableStructuredLogging { get; set; } = true;
    public bool LogRequestDetails { get; set; } = true;
    public bool ExcludeSensitiveData { get; set; } = true;
    public string[] SensitiveHeaders { get; set; } = 
    {
        "Authorization",
        "Cookie", 
        "X-API-Key",
        "X-Auth-Token"
    };
    public string LogLevel { get; set; } = "Information";
    public string FilePath { get; set; } = "logs/admintool-.log";
    public long MaxFileSizeBytes { get; set; } = 10_485_760; // 10MB
    public int RetainedFileCountLimit { get; set; } = 31;
}