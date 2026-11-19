namespace MyMMORPG.Framework.Configuration;

public sealed class NetworkSettings
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 19222;
    public int MaxClient { get; set; } = 1000;
    public int MaxFrameBytes { get; set; } = 1024 * 1024;
}