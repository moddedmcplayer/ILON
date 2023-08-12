namespace Server.Models;

using System.Reflection.Emit;

public class OpCodeResolver
{
    public OpCode? OpCode { get; init; }
    private static Dictionary<short, OpCode>? _allOpCodes;

    public OpCodeResolver(byte data)
    {
        _allOpCodes ??= typeof(OpCodes).GetFields()
            .Select(x => (OpCode)x.GetValue(null)!)
            .ToDictionary(x => x.Value, x => x);

        try
        {
            OpCode = _allOpCodes[data];
        }
        catch
        {
            // ignored
        }
    }
}