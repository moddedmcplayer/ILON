namespace Server.Models;

public class SerializedLocalVariable
{
    public string Type { get; set; } = string.Empty;
    public int Index { get; set; }
    public bool IsPinned { get; set; }
}