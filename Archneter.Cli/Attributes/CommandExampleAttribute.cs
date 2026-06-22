namespace Archneter.Cli.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class CommandExampleAttribute : Attribute
    {
        public string Example { get; }

        public CommandExampleAttribute(string example)
        {
            Example = example;
        }
    }
}
