using Archneter.Core.Abstractions;
using Archneter.Core.Enums;
using Archneter.Generators.CleanArchitecture;

namespace Archneter.Cli.Services
{
    public sealed class GeneratorFactory
    {
        public IArchitectureGenerator Get(ArchitectureType type)
        {
            return type switch
            {
                ArchitectureType.CleanArchitecture
                => new CleanArchitectureGenerator(),

                _ => throw new NotImplementedException(
                    "Architecture not supported yet"
                )
            };
        }
    }
}