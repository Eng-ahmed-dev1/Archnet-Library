using System.Reflection;
using Archnet.Cli.Commands;
using Archnet.Cli.Services;

var commands = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(IArchCommand).IsAssignableFrom(t)
                && t is { IsClass: true, IsAbstract: false }
                ).Select(t => (IArchCommand)Activator.CreateInstance(t)!)
                .ToList();

var dispatcher = new CommandDispatcher(commands);
if (args.Length == 0)
{
    Console.WriteLine("Usage : archnet <command>");
    return;
}
var command = args[0];
var commandArgs = args.Skip(1).ToArray();

await dispatcher.DispatchAsync(command, commandArgs);