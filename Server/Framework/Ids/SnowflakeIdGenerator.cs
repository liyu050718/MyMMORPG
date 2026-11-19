namespace MyMMORPG.Framework.Ids;

public sealed class SnowflakeIdGenerator : IIdGenerator
{
    // 2025-01-01T00:00:00Z
    private const long Epoch = 1735689600000L;
    private const int WorkerIdBits = 10;
    private const int SequenceBits = 12;
    private const long MaxWorkerId = (1L << WorkerIdBits) - 1;
    private const long SequenceMask = (1L << SequenceBits) - 1;

    private readonly object _lock = new();
    private readonly long _workerId;
    private long _lastTimestamp = -1L;
    private long _sequence;

    public SnowflakeIdGenerator(int workerId)
    {
        if (workerId is < 0 or > (int)MaxWorkerId)
        {
            throw new ArgumentOutOfRangeException(nameof(workerId), $"workerId 必须在 0~{MaxWorkerId} 之间");
        }

        _workerId = workerId;
    }

    public long NextId()
    {
        lock (_lock)
        {
            var timestamp = CurrentMillis();

            if (timestamp < _lastTimestamp)
            {
                var diff = _lastTimestamp - timestamp;
                if (diff > 5_000)
                {
                    throw new InvalidOperationException($"系统时钟回拨 {diff}ms，拒绝生成 ID");
                }

                Thread.Sleep((int)diff);
                timestamp = CurrentMillis();
            }

            if (timestamp == _lastTimestamp)
            {
                _sequence = (_sequence + 1) & SequenceMask;
                if (_sequence == 0)
                {
                    while (timestamp <= _lastTimestamp)
                    {
                        Thread.SpinWait(100);
                        timestamp = CurrentMillis();
                    }
                }
            }
            else
            {
                _sequence = 0;
            }

            _lastTimestamp = timestamp;

            return ((timestamp - Epoch) << (WorkerIdBits + SequenceBits))
                   | (_workerId << SequenceBits)
                   | _sequence;
        }
    }

    private static long CurrentMillis() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}