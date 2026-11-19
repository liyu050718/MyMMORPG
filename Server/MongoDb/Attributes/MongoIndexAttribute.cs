namespace MyMMORPG.MongoDb.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class MongoIndexAttribute : Attribute
{
    public string FieldName { get; }

    public bool Unique { get; set; }

    public bool Descending { get; set; }

    public MongoIndexAttribute(string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            throw new ArgumentException("索引字段名不能为空", nameof(fieldName));
        }

        FieldName = fieldName;
    }
}
