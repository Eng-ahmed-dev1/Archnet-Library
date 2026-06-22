namespace Archneter.Generators.Infrastructure
{
    public interface ICliService
    {
        Task RunAsync(string arguments, string workingDirectory);
        Task CreateProjectAsync(string template, string projectName, string outputPath);
        Task AddToSolutionAsync(string slnPath, string projectPath);
        Task AddReferenceAsync(string fromProjectPath, string toProjectPath);
    }
}