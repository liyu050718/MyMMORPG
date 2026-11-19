using Microsoft.Extensions.Configuration;
using MyMMORPG.Shared.Protocol;

namespace MyMMORPG.Framework.Configuration;

public static class ServerConfiguration
{
    public static AppLaunchSettings AppLaunchSettings { get; private set; } = null!;
    public static DbSettings DbSettings { get; private set; } = null!;
    public static NetworkSettings NetworkSettings { get; private set; } = null!;
    public static LogSettings LogSettings { get; private set; } = null!;

    public static void Initialize(string[] args)
    {
        var basePath = AppContext.BaseDirectory;

        AppLaunchSettings = LoadFile<AppLaunchSettings>(basePath, "ServerConfig/AppLaunchSettings.json");
        DbSettings = LoadFile<DbSettings>(basePath, AppLaunchSettings.DBConfig);
        NetworkSettings = LoadFile<NetworkSettings>(basePath, AppLaunchSettings.NetworkConfig);
        LogSettings = LoadFile<LogSettings>(basePath, AppLaunchSettings.LogConfig);

        Validate();
    }
    private static T LoadFile<T>(string basePath, string relativePath) where T : class
    {
        var fullPath = Path.GetFullPath(Path.Combine(basePath, relativePath));
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"配置文件不存在：{fullPath}", fullPath);
        }
        var directory = Path.GetDirectoryName(fullPath)!;
        var fileName = Path.GetFileName(fullPath);
        var configuration = new ConfigurationBuilder().SetBasePath(directory).AddJsonFile(fileName, optional: false, reloadOnChange: false).Build();
        return configuration.Get<T>() ?? throw new InvalidOperationException($"配置文件反序列化失败: {fullPath}");
    }
    private static void Validate()
    {
        if (AppLaunchSettings.SnowflakeWorkerId is < 0 or > 1023)
        {
            throw new InvalidOperationException("SnowflakeWorkerId 必须在 0~1023 之间");
        }

        if (string.IsNullOrWhiteSpace(DbSettings.ConnectionString))
        {
            throw new InvalidOperationException("DbConfig.ConnectionString 不能为空");
        }

        if (string.IsNullOrWhiteSpace(DbSettings.Database))
        {
            throw new InvalidOperationException("DbConfig.Database 不能为空");
        }

        if (NetworkSettings.Port is < 0 or > 65535)
        {
            throw new InvalidOperationException("NetworkConfig.Port 必须在 0~65535 之间");
        }

        if (NetworkSettings.MaxClient <= 0)
        {
            throw new InvalidOperationException("NetworkConfig.MaxClient 必须大于 0");
        }

        if (NetworkSettings.MaxFrameBytes < NetPacket.HeaderSize)
        {
            throw new InvalidOperationException("NetworkConfig.MaxFrameBytes 太小");
        }
    }
}



