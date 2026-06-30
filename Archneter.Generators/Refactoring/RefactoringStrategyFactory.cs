using Archneter.Core.Enums;
using Archneter.Generators.Refactoring.Strategies;
using Archneter.Generators.Infrastructure;

namespace Archneter.Generators.Refactoring;

/// <summary>
/// Resolves the appropriate <see cref="IRefactoringStrategy"/> based on the target architecture.
/// Mirrors the existing GeneratorFactory pattern.
/// </summary>
public class RefactoringStrategyFactory
{
    private readonly DotnetCliService _realCli;
    private readonly DryRunCliService _dryRunCli;

    public RefactoringStrategyFactory(DotnetCliService realCli, DryRunCliService dryRunCli)
    {
        _realCli = realCli;
        _dryRunCli = dryRunCli;
    }

    public virtual IRefactoringStrategy Create(ArchitectureType target, bool isDryRun)
    {
        ICliService cli = isDryRun ? _dryRunCli : _realCli;

        return target switch
        {
            ArchitectureType.CleanArchitecture => new ToCleanArchitectureStrategy(cli),
            ArchitectureType.VerticalSlice     => new ToVerticalSliceStrategy(cli),
            ArchitectureType.ModularMonolith   => new ToModularMonolithStrategy(cli),
            ArchitectureType.Microservices     => new ToMicroservicesStrategy(cli),
            ArchitectureType.NTier             => new ToNTierStrategy(cli),
            ArchitectureType.Api               => new ToApiStrategy(cli),
            _ => throw new NotSupportedException($"No refactoring strategy for: {target}")
        };
    }
}