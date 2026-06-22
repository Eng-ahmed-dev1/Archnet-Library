using Archneter.Cli.Commands;

namespace Archneter.Cli.Models
{
    public class CommandDescriptor
    {
        public string Name { get; set; } = string.Empty;

        public Type CommandType { get; set; } = default!;
    }
}