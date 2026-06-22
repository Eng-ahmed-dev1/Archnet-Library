namespace Archneter.Cli.Models
{
    public class CommandContext
    {
        public string Command { get; set; } = string.Empty;

        public string ProjectName { get; set; } = string.Empty;

        public Dictionary<string, string> Options { get; set; } = new();

        /// <summary>
        /// Boolean flags that appear without a value, e.g. --dry-run
        /// </summary>
        public HashSet<string> Flags { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}