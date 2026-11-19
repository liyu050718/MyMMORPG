namespace MyMMORPG.MongoDb.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]

public sealed class MongodbCollectionDefineAttribute : Attribute
{
    public string CollectionName { get; }

    public MongodbCollectionDefineAttribute(string collectionName)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            throw new ArgumentException("集合名不能为空", nameof(collectionName));
        }

        CollectionName = collectionName;
    }

}