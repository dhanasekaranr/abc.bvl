namespace abc.bvl.AdminTool.Api.Configuration;

/// <summary>
/// Database configuration settings
/// </summary>
public class DatabaseSettings
{
    public const string SectionName = "Database";
    
    /// <summary>
    /// Database provider: "Oracle" for production, "InMemory" for testing
    /// </summary>
    public string Provider { get; set; } = "Oracle";
    
    /// <summary>
    /// Enable EF Core migrations (should be false for Oracle - managed externally)
    /// </summary>
    public bool EnableMigrations { get; set; } = false;
}
