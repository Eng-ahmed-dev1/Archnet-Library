using Archnet.Cli.Attributes;
using Archnet.Cli.Services;
using Archnet.Core.Enums;
using Archnet.Core.Models;

namespace Archnet.Cli.Commands;

[Command("new")]
public class NewCommand : IArchCommand
{
    public async Task ExecuteAsync(CommandContext context)
    {
        var projectName = context.ProjectName;

        var arch = context.Options.GetValueOrDefault("--arch", "clean");
        var tests = context.Options.GetValueOrDefault("--tests", "false") == "true";

        var options = new ProjectOptions
        {
            ProjectName = projectName,
            Architecture = ArchitectureType.CleanArchitecture,
            GenerateTests = tests
        };

        var factory = new GeneratorFactory();
        var generator = factory.Get(options.Architecture);

        await generator.GenerateAsync(options);

        Console.WriteLine("Project Created Successfully..");
    }
}