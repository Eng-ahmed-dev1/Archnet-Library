namespace Archneter.Generators.Infrastructure
{
    public class DryRunCliService : ICliService
    {
        private static readonly string _separator = new string('─', 60);
        private int _stepIndex = 0;

        public Task RunAsync(string arguments, string workingDirectory)
        {
            _stepIndex++;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  [{_stepIndex:D2}] dotnet {arguments}");
            Console.ResetColor();
            return Task.CompletedTask;
        }

        public Task CreateProjectAsync(string template, string projectName, string outputPath)
        {
            PrintSection($"create project  {projectName}  ({template})");
            return RunAsync($"new {template} -n {projectName} -o \"{outputPath}\"", Directory.GetCurrentDirectory());
        }

        public Task AddToSolutionAsync(string slnPath, string projectPath)
        {
            var projectName = Path.GetFileName(projectPath);
            PrintSection($"add to solution  →  {projectName}");
            return RunAsync($"sln \"{slnPath}\" add \"{projectPath}\"", Directory.GetCurrentDirectory());
        }

        public Task AddReferenceAsync(string fromProjectPath, string toProjectPath)
        {
            var from = Path.GetFileNameWithoutExtension(fromProjectPath);
            var to = Path.GetFileNameWithoutExtension(toProjectPath);
            PrintSection($"reference  {from}  →  {to}");
            return RunAsync($"add \"{fromProjectPath}\" reference \"{toProjectPath}\"", Directory.GetCurrentDirectory());
        }

        private void PrintSection(string label)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n  ▸ {label}");
            Console.ResetColor();
        }

        public void PrintHeader(string projectName, string architecture)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n  {_separator}");
            Console.WriteLine($"  DRY RUN — {architecture}  »  {projectName}");
            Console.WriteLine($"  {_separator}");
            Console.ResetColor();
        }

        public void PrintFooter()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n  {_separator}");
            Console.WriteLine($"  {_stepIndex} command(s) would be executed. No files were created.");
            Console.WriteLine($"  Run without --dry-run to apply.\n");
            Console.ResetColor();
        }
    }
}