namespace Archneter.Cli.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class CommandOptionAttribute : Attribute
    {
        public string Template { get; }
        public string Description { get; }
        public string[] Details { get; }

        public CommandOptionAttribute(string template, string description, params string[] details)
        {
            Template = template;
            Description = description;
            Details = details ?? Array.Empty<string>();
        }
    }
}
