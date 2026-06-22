using Archneter.Cli.Models;

namespace Archneter.Cli.Commands
{
    public interface IArchCommand
    {
        Task ExecuteAsync(CommandContext context);
    }
}