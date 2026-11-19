namespace MyMMORPG.Framework.Configuration;

public sealed class AppLaunchSettings
{
    public string DBConfig { get; set; } = "./ServerConfig/db.json";
    public string NetworkConfig { get; set; } = "./ServerConfig/Network.json";
    public string LogConfig { get; set; } = "./ServerConfig/LogConfig.json";
    public int SnowflakeWorkerId { get; set; } = 1;
}