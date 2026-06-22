namespace Archneter.Cli.Models
{
    public sealed class OptionMetadata
    {
        public string Template { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Details { get; set; } = new();
    }
}
