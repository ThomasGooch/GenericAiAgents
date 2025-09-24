namespace Agent.Tools;

[AttributeUsage(AttributeTargets.Class)]
public class ToolAttribute : Attribute
{
    public string Name { get; }

    public ToolAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class DescriptionAttribute : Attribute
{
    public string Description { get; }

    public DescriptionAttribute(string description)
    {
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }
}