namespace LlmTornado.Demo;

[AttributeUsage(AttributeTargets.Method)]
public class FlakyAttribute : Attribute
{
    public string? Reason { get; set; }

    public FlakyAttribute(string? reason = null)
    {
        Reason = reason;
    }
}

public class MethodAttribute : Attribute
{
    public string MethodName { get; set; }
    
    public MethodAttribute(string methodName)
    {
        MethodName = methodName;
    }
}

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

public class DemoEnumAttribute : Attribute
{
    public Type DemoType { get; set; }
    
    public DemoEnumAttribute(Type demoType)
    {
        DemoType = demoType;
    }
}