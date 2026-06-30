namespace Archneter.Generators.Infrastructure
{
    /// <summary>
    /// A mock implementation of <see cref="ICliService"/> that simulates command execution by printing the intended commands to the console.
    /// Used for the --dry-run CLI flag to preview scaffolding.
    /// </summary>
    public class DryRunCliService : ICliService
    {
        private static readonly string _separator = new string('─', 60);
        private int _stepIndex = 0;

        /// <summary>
        /// Simulates executing a raw dotnet command.
        /// </summary>
        /// <param name="arguments">The dotnet command arguments.</param>
        /// <param name="workingDirectory">The working directory context.</param>
        public Task RunAsync(string arguments, string workingDirectory)
        {
            _stepIndex++;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  [{_stepIndex:D2}] dotnet {arguments}");
            Console.ResetColor();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Simulates scaffolding a new dotnet project.
        /// </summary>
        public Task CreateProjectAsync(string template, string projectName, string outputPath)
        {
            PrintSection($"create project  {projectName}  ({template})");
            return RunAsync($"new {template} -n {projectName} -o \"{outputPath}\"", Directory.GetCurrentDirectory());
        }

        /// <summary>
        /// Simulates adding a project file to a solution.
        /// </summary>
        public Task AddToSolutionAsync(string slnPath, string projectPath)
        {
            var projectName = Path.GetFileName(projectPath);
            PrintSection($"add to solution  →  {projectName}");
            return RunAsync($"sln \"{slnPath}\" add \"{projectPath}\"", Directory.GetCurrentDirectory());
        }

        /// <summary>
        /// Simulates adding a project reference.
        /// </summary>
        public Task AddReferenceAsync(string fromProjectPath, string toProjectPath)
        {
            var from = Path.GetFileNameWithoutExtension(fromProjectPath);
            var to = Path.GetFileNameWithoutExtension(toProjectPath);
            PrintSection($"reference  {from}  →  {to}");
            return RunAsync($"add \"{fromProjectPath}\" reference \"{toProjectPath}\"", Directory.GetCurrentDirectory());
        }

        public Task AddReferencesAsync(string fromProjectPath, IEnumerable<string> toProjectPaths)
        {
            var from = Path.GetFileNameWithoutExtension(fromProjectPath);
            PrintSection($"reference  {from}  →  {toProjectPaths.Count()} projects");
            var refs = string.Join(" ", toProjectPaths.Select(p => $"\"{p}\""));
            return RunAsync($"add \"{fromProjectPath}\" reference {refs}", Directory.GetCurrentDirectory());
        }

        public Task AddMultipleToSolutionAsync(string slnPath, IEnumerable<string> projectPaths)
        {
            PrintSection($"add to solution  →  {projectPaths.Count()} projects");
            var projs = string.Join(" ", projectPaths.Select(p => $"\"{p}\""));
            return RunAsync($"sln \"{slnPath}\" add {projs}", Directory.GetCurrentDirectory());
        }

        public Task AddPackageAsync(string projectPath, string packageName, string version = null)
        {
            var proj = Path.GetFileName(projectPath);
            var versionStr = string.IsNullOrWhiteSpace(version) ? "" : $" (v{version})";
            PrintSection($"add package  {packageName}{versionStr}  →  {proj}");
            var versionArg = string.IsNullOrWhiteSpace(version) ? "" : $" --version \"{version}\"";
            return RunAsync($"add \"{projectPath}\" package {packageName}{versionArg} --no-restore", Directory.GetCurrentDirectory());
        }

        public Task RestoreProjectAsync(string projectOrSlnPath)
        {
            var target = Path.GetFileName(projectOrSlnPath);
            PrintSection($"restore  →  {target}");
            return RunAsync($"restore \"{projectOrSlnPath}\"", Directory.GetCurrentDirectory());
        }

        private void PrintSection(string label)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n  ▸ {label}");
            Console.ResetColor();
        }

        /// <summary>
        /// Prints a formatted header for the dry-run output.
        /// </summary>
        /// <param name="projectName">The name of the project being scaffolded.</param>
        /// <param name="architecture">The architecture type being used.</param>
        public void PrintHeader(string projectName, string architecture)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n  {_separator}");
            Console.WriteLine($"  DRY RUN — {architecture}  »  {projectName}");
            Console.WriteLine($"  {_separator}");
            Console.ResetColor();
        }

        /// <summary>
        /// Prints a formatted footer summarizing the simulated execution steps.
        /// </summary>
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