using System.Reflection;
using Archneter.Cli.Attributes;
using Archneter.Cli.Commands;
using Archneter.Cli.Models;
using Archneter.Cli.Parsing;
using Archneter.Cli.Services;

var commands = CommandRegistry.Instance.GetCommands();

var dispatcher = new CommandDispatcher(commands);

if (args.Length == 0)
{
    await dispatcher.DispatchAsync("help", new CommandContext { Command = "help" });
    return;
}

var parser = new ArgumentParser();
var context = parser.Parse(args);

await dispatcher.DispatchAsync(context.Command, context);