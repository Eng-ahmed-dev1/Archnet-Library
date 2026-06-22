using Archneter.Core.Models;

namespace Archneter.Core.Abstractions
{
    public interface IArchitectureGenerator
    {
        Task GenerateAsync(ProjectOptions options);
    }
}