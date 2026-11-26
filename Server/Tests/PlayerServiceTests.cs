using Microsoft.Extensions.Logging.Abstractions;
using MyMMORPG.Framework.Ids;
using MyMMORPG.GameServer.Services.Player;
using MyMMORPG.Shared.Protocol;
using Xunit;

namespace MyMMORPG.Tests;

[Trait("Category", "Mongo")]
public sealed class PlayerServiceTests : IClassFixture<MongoFixture>
{
    private readonly PlayerService _service;

    public PlayerServiceTests(MongoFixture fixture)
    {
        _service = new PlayerService(
            fixture.Players,
            new SnowflakeIdGenerator(1),
            NullLogger<PlayerService>.Instance);
    }

    [Fact]
    public async Task Create_Then_Get_And_Update_And_Delete()
    {
        var created = await _service.CreateAsync("Svc_Alice");
        Assert.True(created.PlayerId > 0);
        Assert.Equal(1, created.Level);

        var fetched = await _service.GetAsync(created.PlayerId);
        Assert.Equal("Svc_Alice", fetched.Name);

        var updated = await _service.UpdateAsync(created.PlayerId, 5, 100, 200);
        Assert.Equal(5, updated.Level);
        Assert.Equal(100, updated.Exp);
        Assert.Equal(200, updated.Gold);

        Assert.True(await _service.DeleteAsync(created.PlayerId));

        var ex = await Assert.ThrowsAsync<RpcException>(
            () => _service.GetAsync(created.PlayerId));
        Assert.Equal(ErrorCode.NotFound, ex.Code);
    }

    [Fact]
    public async Task DuplicateName_Throws_AlreadyExists()
    {
        await _service.CreateAsync("Svc_Duplicate");

        var ex = await Assert.ThrowsAsync<RpcException>(
            () => _service.CreateAsync("Svc_Duplicate"));
        Assert.Equal(ErrorCode.AlreadyExists, ex.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345678901234567")]
    public async Task InvalidName_Throws_InvalidArgument(string name)
    {
        var ex = await Assert.ThrowsAsync<RpcException>(
            () => _service.CreateAsync(name));
        Assert.Equal(ErrorCode.InvalidArgument, ex.Code);
    }

    [Fact]
    public async Task InvalidLevel_Throws_InvalidArgument()
    {
        var created = await _service.CreateAsync("Svc_Level");

        var ex = await Assert.ThrowsAsync<RpcException>(
            () => _service.UpdateAsync(created.PlayerId, 0, null, null));
        Assert.Equal(ErrorCode.InvalidArgument, ex.Code);
    }
}
