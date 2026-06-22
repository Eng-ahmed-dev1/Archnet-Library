using System.Reflection;
using Archneter.Cli.Attributes;
using Archneter.Cli.Commands;
using Archneter.Cli.Models;

namespace Archneter.Cli.Services
{
    public sealed class CommandRegistry
    {
        private static readonly Lazy<CommandRegistry> _instance = new(() => new CommandRegistry());
        public static CommandRegistry Instance => _instance.Value;

        private readonly List<CommandDescriptor> _commands;

        private CommandRegistry()
        {
            _commands = DiscoverCommands();
        }

        public IReadOnlyList<CommandDescriptor> GetCommands() => _commands;

        public IEnumerable<CommandMetadata> GetCommandsMetadata()
        {
            return _commands.Select(c => ExtractMetadata(c.Name, c.Command.GetType()));
        }

        private static List<CommandDescriptor> DiscoverCommands()
        {
            return Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(IArchCommand).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
                .Select(t =>
                {
                    var attr = t.GetCustomAttribute<CommandAttribute>();
                    if (attr is null) return null;

                    var instance = (IArchCommand)Activator.CreateInstance(t)!;
                    return new CommandDescriptor { Name = attr.Name, Command = instance };
                })
                .Where(x => x is not null)
                .Cast<CommandDescriptor>()
                .ToList();
        }

        private static CommandMetadata ExtractMetadata(string name, Type type)
        {
            var descAttr = type.GetCustomAttribute<DescriptionAttribute>();
            var syntaxAttr = type.GetCustomAttribute<CommandSyntaxAttribute>();
            var optionAttrs = type.GetCustomAttributes<CommandOptionAttribute>();
            var exampleAttrs = type.GetCustomAttributes<CommandExampleAttribute>();

            return new CommandMetadata
            {
                Name = name,
                Description = descAttr?.Text ?? string.Empty,
                Syntax = syntaxAttr?.Syntax ?? name,
                Options = optionAttrs.Select(o => new OptionMetadata
                {
                    Template = o.Template,
                    Description = o.Description,
                    Details = o.Details.ToList()
                }).ToList(),
                Examples = exampleAttrs.Select(e => e.Example).ToList()
            };
        }
    }
}
