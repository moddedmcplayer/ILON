namespace Server.Models;

public class InlinedReferences
{
    public SerializedTypeInfo[] InlinedTypes { get; set; }
    public SerializedMethodInfo[] InlinedMethods { get; set; }
    public string[] InlinedStrings { get; set; }

    // For deserialization
#pragma warning disable CS8618
    public InlinedReferences()
#pragma warning restore CS8618
    {
    }

    public InlinedReferences(SerializedTypeInfo[] inlinedTypes, SerializedMethodInfo[] inlinedMethods, string[] inlinedStrings)
    {
        InlinedTypes = inlinedTypes;
        InlinedMethods = inlinedMethods;
        InlinedStrings = inlinedStrings;
    }
}