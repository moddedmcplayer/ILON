namespace Client;

using System.Reflection;
using Server.Models;

public static class Extensions
{
    public static SerializedLocalVariable[] SerializeLocalVars(this IEnumerable<LocalVariableInfo> vars)
        => vars.Select(x => new SerializedLocalVariable
        {
            Type = x.LocalType.FullName ?? throw new ArgumentException("LocalType.FullName is null"),
            Index = x.LocalIndex,
            IsPinned = x.IsPinned
        }).ToArray();
}