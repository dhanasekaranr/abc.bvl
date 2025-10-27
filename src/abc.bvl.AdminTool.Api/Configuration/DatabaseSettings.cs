namespace abc.bvl.AdminTool.Api.Configuration;

public class DatabaseSettings
{
    public const string SectionName = "Database";
    
    public string Provider { get; set; } = "InMemory"; // InMemory, Oracle
    public bool EnableSeeding { get; set; } = true;
    public bool EnableMigrations { get; set; } = false;
}