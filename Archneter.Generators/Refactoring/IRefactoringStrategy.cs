using Archneter.Core.Models;

namespace Archneter.Generators.Refactoring;

public interface IRefactoringStrategy
{
    Task ExecuteAsync(RefactorOptions options, AnalyzedProject analyzedProject);
}
