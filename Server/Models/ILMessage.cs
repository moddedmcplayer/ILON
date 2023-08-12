namespace Server.Models;

using System.Reflection;

public class ILMessage
{
    public Event Event { get; set; }
    public string[]? ClassGenericArguments { get; set; }
    public string[]? MethodGenericArguments { get; set; }
    public SerializedLocalVariable[] LocalsVariables { get; set; }
    public InlinedReferences? InlinedReferences { get; set; }
    public byte[] Instructions { get; set; }

    // For deserialization
#pragma warning disable CS8618
    public ILMessage()
#pragma warning restore CS8618
    {
    }

    public ILMessage(Event @event, MethodInfo info)
    {
        Event = @event;

        var body = info.GetMethodBody();
        if (body != null)
        {
            LocalsVariables = body.LocalVariables.SerializeLocalVars();
            var bytes = body.GetILAsByteArray()!;
            Instructions = bytes;
            InlinedReferences = this.EmitTo(null, Assembly.GetCallingAssembly().ManifestModule);
        }
        else
        {
            LocalsVariables = Array.Empty<SerializedLocalVariable>();
            Instructions = Array.Empty<byte>();
            InlinedReferences = null;
        }

        if (info.DeclaringType?.IsGenericType ?? false)
        {
            ClassGenericArguments = info.DeclaringType.GetGenericArguments()
                .Select(x => x.FullName!)
                .ToArray();
        }

        if (info.IsGenericMethod)
        {
            MethodGenericArguments = info.GetGenericArguments()
                .Select(x => x.FullName!)
                .ToArray();
        }
    }
}