using MyMMORPG.Framework.Ids;
using Xunit;

namespace MyMMORPG.Tests;

public sealed class SnowflakeIdGeneratorTests
{
    [Fact]
    public void Generate_1000_Ids_Are_Unique_And_Positive()
    {
        var generator = new SnowflakeIdGenerator(1);
        var ids = Enumerable.Range(0, 1000).Select(_ => generator.NextId()).ToArray();

        Assert.Equal(1000, ids.Distinct().Count());
        Assert.All(ids, id => Assert.True(id > 0));
    }

    [Fact]
    public void Two_Workers_Produce_Different_Ids()
    {
        var a = new SnowflakeIdGenerator(1);
        var b = new SnowflakeIdGenerator(2);

        Assert.NotEqual(a.NextId(), b.NextId());
    }

    [Fact]
    public void Invalid_WorkerId_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SnowflakeIdGenerator(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SnowflakeIdGenerator(1024));
    }
}
