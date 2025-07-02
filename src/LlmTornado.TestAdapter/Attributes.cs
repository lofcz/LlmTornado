namespace LlmTornado.TestAdapter;

[AttributeUsage(AttributeTargets.Method)]
public class TornadoTestAttribute : Attribute
{
    public string? FriendlyName { get; set; }
    
    public TornadoTestAttribute()
    {

    }
    
    public TornadoTestAttribute(string friendlyName)
    {
        FriendlyName = friendlyName;
    }
}