using System.Reflection;
using Archnet.Cli.Attributes;
using Archnet.Cli.Commands;
using Archnet.Cli.Models;
using Archnet.Cli.Services;

var commands =
    Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(t =>
            typeof(IArchCommand).IsAssignableFrom(t) &&
            t is { IsClass: true, IsAbstract: false })
        .Select(t =>
        {
            var attr = t.GetCustomAttribute<CommandAttribute>();

            if (attr is null)
                return null;

            var instance = (IArchCommand)Activator.CreateInstance(t)!;

            return new CommandDescriptor
            {
                Name = attr.Name,
                Command = instance
            };
        })
        .Where(x => x is not null)
        .Cast<CommandDescriptor>()
        .ToList();

var dispatcher = new CommandDispatcher(commands);

if (args.Length == 0)
{
    Console.WriteLine("Usage: archnet <command>");
    return;
}

var command = args[0];
var commandArgs = args.Skip(1).ToArray();
var context = new CommandContext
{
    Command = command
};
await dispatcher.DispatchAsync(command, context);