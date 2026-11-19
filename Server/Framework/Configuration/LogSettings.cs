using Microsoft.Extensions.Logging;

namespace MyMMORPG.Framework.Configuration;

public sealed class LogSettings
{
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;
}