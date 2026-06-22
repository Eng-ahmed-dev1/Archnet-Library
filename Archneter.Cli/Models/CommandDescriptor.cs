using Archneter.Cli.Commands;

namespace Archneter.Cli.Models
{
    public class CommandDescriptor
    {
        public string Name { get; set; } = string.Empty;

        public IArchCommand Command { get; set; } = default!;
    }
}