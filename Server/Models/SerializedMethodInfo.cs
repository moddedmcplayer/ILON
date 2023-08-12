namespace Server.Models;

using System.Reflection;

public class SerializedMethodInfo
{
    public string MethodName { get; set; }
    public string[] GenericArguments { get; set; }
    public string[] Arguments { get; set; }
    public string DeclaringType { get; set; }

    // For deserialization
#pragma warning disable CS8618
    public SerializedMethodInfo()
#pragma warning restore CS8618
    {
    }

    public SerializedMethodInfo(MethodBase info)
    {
        MethodName = info.Name;
        GenericArguments = info.IsGenericMethod
            ? info.GetGenericArguments()
                .Select(x => x.FullName!)
                .ToArray()
            : Array.Empty<string>();
        Arguments = info.GetParameters()
            .Select(x => x.ParameterType.FullName!)
            .ToArray();
        DeclaringType = info.DeclaringType?.FullName ?? string.Empty;
    }
}