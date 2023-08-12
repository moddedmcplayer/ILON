namespace Server.Models;

public class SerializedTypeInfo
{
    public string TypeName { get; set; }
    public string[] GenericArguments { get; set; }

    // For deserialization
#pragma warning disable CS8618
    public SerializedTypeInfo()
#pragma warning restore CS8618
    {
    }

    public SerializedTypeInfo(Type type)
    {
        TypeName = type.FullName ?? throw new Exception("Generic type parameters are not supported");
        GenericArguments = type.IsGenericType
            ? type.GetGenericArguments()
                .Select(x => x.FullName!)
                .ToArray()
            : Array.Empty<string>();
    }
}