namespace Archneter.Cli.Models
{
    public sealed class CommandMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Syntax { get; set; } = string.Empty;
        public List<OptionMetadata> Options { get; set; } = new();
        public List<string> Examples { get; set; } = new();
    }
}
