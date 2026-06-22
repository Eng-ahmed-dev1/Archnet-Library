using Microsoft.Extensions.DependencyInjection;
using Archneter.Cli.Commands;
using Archneter.Cli.Models;

namespace Archneter.Cli.Services
{
    /// <summary>
    /// Responsible for routing parsed arguments to the appropriate command execution logic.
    /// </summary>
    public sealed class CommandDispatcher
    {
        private readonly IEnumerable<CommandDescriptor> _commands;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandDispatcher"/> class.
        /// </summary>
        /// <param name="registry">The registry containing all available commands.</param>
        /// <param name="serviceProvider">The service provider to resolve command instances.</param>
        public CommandDispatcher(CommandRegistry registry, IServiceProvider serviceProvider)
        {
            _commands = registry.GetCommands();
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Dispatches the execution to the targeted command.
        /// </summary>
        /// <param name="command">The name of the command to execute.</param>
        /// <param name="context">The parsed arguments and options context.</param>
        public async Task DispatchAsync(string command, CommandContext context)
        {
            var target =
                _commands.FirstOrDefault(x => x.Name == command);

            if (target is null)
            {
                Console.WriteLine($"Unknown Command: {command}");
                return;
            }

            var instance = (IArchCommand)_serviceProvider.GetRequiredService(target.CommandType);
            await instance.ExecuteAsync(context);
        }
    }
}