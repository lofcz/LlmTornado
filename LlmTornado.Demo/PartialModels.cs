namespace LlmTornado.Demo;

[AttributeUsage(AttributeTargets.Field)]
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

public class DemoEnumAttribute : Attribute
{
    public Type DemoType { get; set; }
    
    public DemoEnumAttribute(Type demoType)
    {
        DemoType = demoType;
    }
}