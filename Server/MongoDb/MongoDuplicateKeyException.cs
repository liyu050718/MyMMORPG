namespace MyMMORPG.MongoDb;

public sealed class MongoDuplicateKeyException : Exception
{
    public MongoDuplicateKeyException(string message, Exception innerException) : base(message, innerException)
    {

    }
}