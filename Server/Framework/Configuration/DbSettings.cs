namespace MyMMORPG.Framework.Configuration;

public sealed class DbSettings
{
    public string ConnectionString { get; set; } = "mongodb://127.0.0.1:27017";
    public string Database { get; set; } = "mymmorpg";
    public int ServerSelectionTimeoutSeconds { get; set; } = 5;
}