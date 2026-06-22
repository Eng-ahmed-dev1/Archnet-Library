using Archneter.Core.Abstractions;
using Archneter.Core.Enums;
using Archneter.Generators.CleanArchitecture;
using Archneter.Generators.Infrastructure;

namespace Archneter.Cli.Services
{
    public class GeneratorFactory
    {
        private readonly ICliService _cli;

        public GeneratorFactory(ICliService cli)
        {
            _cli = cli;
        }

        public IArchitectureGenerator Get(ArchitectureType architecture) => architecture switch
        {
            ArchitectureType.CleanArchitecture => new CleanArchitectureGenerator(_cli),
            _ => throw new NotSupportedException($"Architecture '{architecture}' is not yet supported.")
        };
    }
}