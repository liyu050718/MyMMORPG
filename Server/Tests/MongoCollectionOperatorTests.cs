using MyMMORPG.GameServer.Data;
using MyMMORPG.MongoDb;
using Xunit;

namespace MyMMORPG.Tests;

[Trait("Category", "Mongo")]
public sealed class MongoCollectionOperatorTests : IClassFixture<MongoFixture>
{
    private readonly MongoFixture _fixture;

    public MongoCollectionOperatorTests(MongoFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Insert_Find_Replace_Delete_Works()
    {
        var player = NewPlayer("Repo_Alice");

        await _fixture.Players.InsertAsync(player);

        var found = await _fixture.Players.FindByIdAsync(player.Id);
        Assert.NotNull(found);
        Assert.Equal(player.Name, found!.Name);

        player.Level = 9;
        player.UpdatedAt = DateTime.UtcNow;
        Assert.True(await _fixture.Players.ReplaceAsync(player));

        var updated = await _fixture.Players.FindByIdAsync(player.Id);
        Assert.Equal(9, updated!.Level);

        Assert.True(await _fixture.Players.DeleteAsync(player.Id));
        Assert.Null(await _fixture.Players.FindByIdAsync(player.Id));
    }

    [Fact]
    public async Task DuplicateName_Throws_DuplicateKey()
    {
        await _fixture.Players.InsertAsync(NewPlayer("Repo_Duplicate"));
        var another = NewPlayer("Repo_Duplicate");

        await Assert.ThrowsAsync<MongoDuplicateKeyException>(
            () => _fixture.Players.InsertAsync(another));
    }

    [Fact]
    public async Task FindLatest_Returns_Descending_By_Id()
    {
        await _fixture.Players.InsertAsync(NewPlayer("Repo_Latest_1"));
        await _fixture.Players.InsertAsync(NewPlayer("Repo_Latest_2"));

        var latest = await _fixture.Players.FindLatestAsync(2);
        Assert.Equal(2, latest.Count);
        Assert.True(latest[0].Id > latest[1].Id);
    }

    private static Player NewPlayer(string name)
        => new()
        {
            Id = Random.Shared.NextInt64(1, long.MaxValue),
            Name = name,
            Level = 1,
            Exp = 0,
            Gold = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
}
