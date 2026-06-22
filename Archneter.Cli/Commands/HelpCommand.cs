using Archneter.Cli.Attributes;
using Archneter.Cli.Models;

namespace Archneter.Cli.Commands;

[Command("help")]
[Description("Display available commands")]
public sealed class HelpCommand : IArchCommand
{
    public Task ExecuteAsync(CommandContext context)
    {
        Console.WriteLine();
        Console.WriteLine("Archneter CLI");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  archneter <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine();
        Console.WriteLine("  help                         Display available commands");
        Console.WriteLine("  new <name> [options]         Create a new architecture project");
        Console.WriteLine();
        Console.WriteLine("Options for 'new':");
        Console.WriteLine();
        Console.WriteLine("  --arch <type>                Architecture type (default: clean)");
        Console.WriteLine("    clean                      Clean Architecture");
        Console.WriteLine("    microservices              Microservices");
        Console.WriteLine();
        Console.WriteLine("  --tests <true|false>         Generate test projects (default: false)");
        Console.WriteLine();
        Console.WriteLine("  --dry-run                    Preview commands without creating any files");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine();
        Console.WriteLine("  archneter new MyProject --arch clean");
        Console.WriteLine("  archneter new MyProject --arch clean --tests true");
        Console.WriteLine("  archneter new MyProject --arch microservices --tests true");
        Console.WriteLine("  archneter new MyProject --arch clean --dry-run");
        Console.WriteLine("  archneter new MyProject --arch clean --tests true --dry-run");
        Console.WriteLine();

        return Task.CompletedTask;
    }
}