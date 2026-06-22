namespace Archneter.Cli.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CommandSyntaxAttribute : Attribute
    {
        public string Syntax { get; }

        public CommandSyntaxAttribute(string syntax)
        {
            Syntax = syntax;
        }
    }
}
