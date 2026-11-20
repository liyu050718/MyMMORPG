using MyMMORPG.MongoDb;
using MyMMORPG.MongoDb.Attributes;

namespace MyMMORPG.GameServer.Data;

[MongodbCollectionDefine("player")]
[MongoIndex(nameof(Name), Unique = true)]
public sealed class Player : IMongoEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public long Exp { get; set; }
    public long Gold { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}