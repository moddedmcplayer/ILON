namespace Server.Models;

public class InlinedReferences
{
    public string[] InlinedMethods { get; set; }
    public SerializedMethodInfo[] Calls { get; set; }

    // For deserialization
#pragma warning disable CS8618
    public InlinedReferences()
#pragma warning restore CS8618
    {
    }

    public InlinedReferences(string[] inlinedMethods, SerializedMethodInfo[] calls)
    {
        InlinedMethods = inlinedMethods;
        Calls = calls;
    }
}