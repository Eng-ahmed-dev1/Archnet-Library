using Archneter.Cli.Attributes;
using Archneter.Cli.Models;
using Archneter.Cli.Services;
using Archneter.Core.Enums;
using Archneter.Core.Models;
using Archneter.Generators.Infrastructure;

namespace Archneter.Cli.Commands
{
    [Command("new")]
    [Description("Create a new architecture project")]
    public class NewCommand : IArchCommand
    {
        public async Task ExecuteAsync(CommandContext context)
        {
            if (string.IsNullOrWhiteSpace(context.ProjectName))
            {
                Console.WriteLine("Error: project name is required. Usage: archneter new <name> [--arch clean] [--tests true] [--dry-run]");
                return;
            }

            var archKey = context.Options.GetValueOrDefault("--arch", "clean");
            var tests = context.Options.GetValueOrDefault("--tests", "false") == "true";
            var isDryRun = context.Flags.Contains("--dry-run");

            if (!TryParseArchitecture(archKey, out var architecture))
            {
                Console.WriteLine($"Error: unknown architecture '{archKey}'. Supported: clean");
                return;
            }

            var options = new ProjectOptions
            {
                ProjectName = context.ProjectName,
                Architecture = architecture,
                GenerateTests = tests
            };

            ICliService cli = isDryRun ? new DryRunCliService() : new DotnetCliService();
            var factory = new GeneratorFactory(cli);
            var generator = factory.Get(options.Architecture);

            await generator.GenerateAsync(options);

            if (!isDryRun)
                Console.WriteLine($"Project '{options.ProjectName}' created successfully.");
        }

        private static bool TryParseArchitecture(string key, out ArchitectureType architecture)
        {
            architecture = key.ToLowerInvariant() switch
            {
                "clean" => ArchitectureType.CleanArchitecture,
                "vsa" or "vertical-slice" => ArchitectureType.VerticalSlice,
                "modular" => ArchitectureType.ModularMonolith,
                "microservices" => ArchitectureType.Microservices,
                _ => default
            };

            return key.ToLowerInvariant() is "clean" or "vsa" or "vertical-slice" or "modular" or "microservices";
        }
    }
}