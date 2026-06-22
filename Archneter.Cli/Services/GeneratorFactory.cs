using Archneter.Core.Abstractions;
using Archneter.Core.Enums;
using Archneter.Generators.CleanArchitecture;
using Archneter.Generators.Infrastructure;
using Archneter.Generators.Microservices;
using Archneter.Generators.NTier;
using Archneter.Generators.ModularMonolith;
using Archneter.Generators.VerticalSlice;

namespace Archneter.Cli.Services;

public static class GeneratorFactory
{
    public static IArchitectureGenerator Create(ArchitectureType type, bool isDryRun = false)
    {
        ICliService cli = isDryRun
            ? new DryRunCliService()
            : new DotnetCliService();

        return type switch
        {
            ArchitectureType.CleanArchitecture => new CleanArchitectureGenerator(cli),
            ArchitectureType.Microservices => new MicroservicesGenerator(cli),
            ArchitectureType.NTier => new NTierArchitectureGenerator(cli),
            ArchitectureType.ModularMonolith => new ModularMonolithArchitectureGenerator(cli),
            ArchitectureType.VerticalSlice => new VerticalSliceArchitectureGenerator(cli),
            _ => throw new NotSupportedException($"Architecture '{type}' is not supported yet.")
        };
    }
}