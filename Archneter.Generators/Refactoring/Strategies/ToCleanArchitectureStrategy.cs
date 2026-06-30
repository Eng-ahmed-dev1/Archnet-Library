using Archneter.Core.Models;
using Archneter.Generators.Infrastructure;

namespace Archneter.Generators.Refactoring.Strategies;

/// <summary>
/// Refactors an existing project into a 4-layer Clean Architecture solution:
///   ProjectName.Domain
///   ProjectName.Application
///   ProjectName.Infrastructure
///   ProjectName.Api
/// </summary>
public class ToCleanArchitectureStrategy : BaseRefactoringStrategy
{
    public ToCleanArchitectureStrategy(ICliService cli) : base(cli) { }
    public override async Task ExecuteAsync(RefactorOptions options, AnalyzedProject analyzedProject)
    {
        var name     = analyzedProject.ProjectName;
        var root     = options.ProjectDirectory;
        var isDryRun = options.DryRun;
        IsDryRun = isDryRun;

        WriteHeader($"Refactoring → Clean Architecture  [{name}]");

        // ── 1. Backup ──────────────────────────────────────────────────────
        if (!options.SkipBackup)
            CreateBackup(root, isDryRun);

        // ── 2. Create the new .NET projects ───────────────────────────────
        WriteHeader("Creating projects");

        var layers = new[]
        {
            ($"{name}.Domain",         "classlib"),
            ($"{name}.Application",    "classlib"),
            ($"{name}.Infrastructure", "classlib"),
            ($"{name}.Api",            "webapi"),
        };

        foreach (var (projectName, template) in layers)
        {
            WriteInfo($"  dotnet new {template} -n {projectName}");
            await RunDotnetAsync(root, "new", template, "-n", projectName, "--force");
        }

        // ── 3. Create / update solution ───────────────────────────────────
        if (!analyzedProject.HasSolution)
        {
            WriteInfo($"  dotnet new sln -n {name}");
            await RunDotnetAsync(root, "new", "sln", "-n", name);
        }

        var slnPath = isDryRun
            ? Path.Combine(root, $"{name}.sln")
            : Directory.GetFiles(root, $"{name}.sln*").FirstOrDefault() ?? Path.Combine(root, $"{name}.sln");

        var projectPaths = layers.Select(l => Path.Combine(l.Item1, $"{l.Item1}.csproj")).ToList();
        await Cli.AddMultipleToSolutionAsync(slnPath, projectPaths.Select(p => Path.Combine(root, p)));

        // ── 4. Wire project references ────────────────────────────────────
        WriteHeader("Adding project references");

        // Application → Domain
        await AddReference(root, $"{name}.Application", $"{name}.Domain");
        // Infrastructure → Domain, Application
        await AddReferences(root, $"{name}.Infrastructure", new[] { $"{name}.Domain", $"{name}.Application" });
        // Api → Application, Infrastructure
        await AddReferences(root, $"{name}.Api", new[] { $"{name}.Application", $"{name}.Infrastructure" });

        // ── 5. Move files to correct layers ──────────────────────────────
        WriteHeader("Moving files");

        foreach (var file in analyzedProject.Files)
        {
            var (targetProject, subFolder) = ResolveDestination(file, name);
            var destDir  = Path.Combine(root, targetProject, subFolder);
            var destPath = Path.Combine(destDir, file.FileName);
            var newNs    = NamespaceRewriter.BuildNamespace(name, LayerSuffix(file.TargetLayer), subFolder);

            file.TargetNamespace = newNs;
            file.SourcePath = MoveFile(file.SourcePath, destPath, name, newNs);
        }

        RewriteNamespaces(analyzedProject.Files);

        if (options.DeepRefactor)
        {
            WriteHeader("Applying Deep Refactoring");
            ApplyDeepRefactoring(analyzedProject.Files);
        }

        foreach (var proj in projectPaths)
        {
            PropagatePackageReferences(analyzedProject, Path.Combine(root, proj));
        }

        if (!isDryRun)
        {
            WriteHeader("Installing Main Packages");
            await PackageInstaller.AddApplicationPackagesAsync(Cli, Path.Combine(root, $"{name}.Application", $"{name}.Application.csproj"));
            await PackageInstaller.AddInfrastructurePackagesAsync(Cli, Path.Combine(root, $"{name}.Infrastructure", $"{name}.Infrastructure.csproj"), Archneter.Core.Enums.DatabaseType.SqlServer);
        }

        // ── 6. Report unclassified files ──────────────────────────────────
        if (analyzedProject.UnclassifiedFiles.Any())
        {
            WriteWarning("\n⚠️  The following files could not be automatically classified:");
            foreach (var f in analyzedProject.UnclassifiedFiles)
                WriteWarning($"   • {Path.GetFileName(f)}  ({f})");

            WriteWarning("   → These files were left in their original location. Move them manually.");
        }

        // ── 7. Done ───────────────────────────────────────────────────────
        WriteHeader("Done");
        WriteSuccess($"✅ Refactoring complete!  Solution: {Path.Combine(root, name + ".sln")}");
        WriteSuccess($"   Moved {analyzedProject.Files.Count} files across 4 layers.");

        if (isDryRun)
            WriteWarning("\n[dry-run] No files were actually created or moved.");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static (string Project, string SubFolder) ResolveDestination(ClassifiedFile file, string name)
    {
        if (file.TargetLayer == ProjectLayer.Presentation && (file.FileName == "Program.cs" || file.FileName == "Startup.cs"))
            return ($"{name}.Api", "");

        return file.TargetLayer switch
        {
            ProjectLayer.Domain         => ($"{name}.Domain",         "Entities"),
            ProjectLayer.Application    => ($"{name}.Application",    ResolveAppSubFolder(file.FileName)),
            ProjectLayer.Infrastructure => ($"{name}.Infrastructure", ResolveInfraSubFolder(file.FileName)),
            ProjectLayer.Presentation   => ($"{name}.Api",            "Controllers"),
            ProjectLayer.Tests          => ($"{name}.Api",            "Tests"),   // Tests get their own project ideally; placed here as fallback
            _                           => ($"{name}.Application",    "Common"),
        };
    }

    private static string ResolveAppSubFolder(string fileName) => fileName switch
    {
        _ when fileName.Contains("Service",   StringComparison.OrdinalIgnoreCase) => "Services",
        _ when fileName.Contains("Handler",   StringComparison.OrdinalIgnoreCase) => "Handlers",
        _ when fileName.Contains("Command",   StringComparison.OrdinalIgnoreCase) => "Commands",
        _ when fileName.Contains("Query",     StringComparison.OrdinalIgnoreCase) => "Queries",
        _ when fileName.Contains("Validator", StringComparison.OrdinalIgnoreCase) => "Validators",
        _ when fileName.Contains("Dto",       StringComparison.OrdinalIgnoreCase) => "DTOs",
        _ when fileName.Contains("Mapping",   StringComparison.OrdinalIgnoreCase) => "Mappings",
        _ => "Common"
    };

    private static string ResolveInfraSubFolder(string fileName) => fileName switch
    {
        _ when fileName.Contains("Repository",  StringComparison.OrdinalIgnoreCase) => "Repositories",
        _ when fileName.Contains("DbContext",   StringComparison.OrdinalIgnoreCase) => "Persistence",
        _ when fileName.Contains("Migration",   StringComparison.OrdinalIgnoreCase) => "Persistence/Migrations",
        _ when fileName.Contains("Configuration",StringComparison.OrdinalIgnoreCase)=> "Persistence/Configurations",
        _ => "Services"
    };

    private static string LayerSuffix(ProjectLayer layer) => layer switch
    {
        ProjectLayer.Domain         => "Domain",
        ProjectLayer.Application    => "Application",
        ProjectLayer.Infrastructure => "Infrastructure",
        ProjectLayer.Presentation   => "Api",
        _                           => "Application"
    };

    private Task AddReference(string root, string from, string to)
    {
        var fromCsproj = Path.Combine(root, from, $"{from}.csproj");
        var toCsproj   = Path.Combine("..",  to,   $"{to}.csproj");
        return Cli.AddReferenceAsync(fromCsproj, Path.Combine(root, from, toCsproj));
    }

    private Task AddReferences(string root, string from, IEnumerable<string> tos)
    {
        var fromCsproj = Path.Combine(root, from, $"{from}.csproj");
        var toCsprojs  = tos.Select(to => Path.Combine(root, from, "..", to, $"{to}.csproj"));
        return Cli.AddReferencesAsync(fromCsproj, toCsprojs);
    }
}