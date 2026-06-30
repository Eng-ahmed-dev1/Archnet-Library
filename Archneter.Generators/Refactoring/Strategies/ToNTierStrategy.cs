using Archneter.Core.Models;
using Archneter.Generators.Infrastructure;

namespace Archneter.Generators.Refactoring.Strategies;

/// <summary>
/// Refactors an existing project into a traditional N-Tier architecture:
///   ProjectName.DAL   (Data Access Layer)
///   ProjectName.BLL   (Business Logic Layer)
///   ProjectName.PL    (Presentation Layer / Web API)
/// </summary>
public class ToNTierStrategy : BaseRefactoringStrategy
{
    public ToNTierStrategy(ICliService cli) : base(cli) { }
    public override async Task ExecuteAsync(RefactorOptions options, AnalyzedProject analyzedProject)
    {
        var name     = analyzedProject.ProjectName;
        var root     = options.ProjectDirectory;
        var isDryRun = options.DryRun;
        IsDryRun = isDryRun;

        WriteHeader($"Refactoring → N-Tier Architecture  [{name}]");

        if (!options.SkipBackup)
            CreateBackup(root, isDryRun);

        // ── Projects ──────────────────────────────────────────────────────
        WriteHeader("Creating projects");

        var layers = new[]
        {
            ($"{name}.DAL", "classlib"),
            ($"{name}.BLL", "classlib"),
            ($"{name}.PL",  "webapi"),
        };

        foreach (var (projectName, template) in layers)
        {
            WriteInfo($"  dotnet new {template} -n {projectName}");
            await RunDotnetAsync(root, "new", template, "-n", projectName, "--force");
        }

        if (!analyzedProject.HasSolution)
            await RunDotnetAsync(root, "new", "sln", "-n", name);

        var slnPath = isDryRun
            ? Path.Combine(root, $"{name}.sln")
            : Directory.GetFiles(root, $"{name}.sln*").FirstOrDefault() ?? Path.Combine(root, $"{name}.sln");

        var projectPaths = layers.Select(l => Path.Combine(l.Item1, $"{l.Item1}.csproj")).ToList();
        await Cli.AddMultipleToSolutionAsync(slnPath, projectPaths.Select(p => Path.Combine(root, p)));

        // ── References ────────────────────────────────────────────────────
        WriteHeader("Adding project references");

        await Cli.AddReferenceAsync(Path.Combine(root, $"{name}.BLL", $"{name}.BLL.csproj"), Path.Combine(root, $"{name}.BLL", "..", $"{name}.DAL", $"{name}.DAL.csproj"));
        await Cli.AddReferenceAsync(Path.Combine(root, $"{name}.PL", $"{name}.PL.csproj"), Path.Combine(root, $"{name}.PL", "..", $"{name}.BLL", $"{name}.BLL.csproj"));

        // ── Move files ────────────────────────────────────────────────────
        WriteHeader("Moving files");

        foreach (var file in analyzedProject.Files)
        {
            var (targetProject, subFolder) = ResolveDestination(file, name);
            var destPath = Path.Combine(root, targetProject, subFolder, file.FileName);
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
            await PackageInstaller.AddApplicationPackagesAsync(Cli, Path.Combine(root, $"{name}.BusinessLogic", $"{name}.BusinessLogic.csproj"));
            await PackageInstaller.AddInfrastructurePackagesAsync(Cli, Path.Combine(root, $"{name}.DataAccess", $"{name}.DataAccess.csproj"), Archneter.Core.Enums.DatabaseType.SqlServer);
        }

        ReportUnclassified(analyzedProject);

        WriteHeader("Done");
        WriteSuccess($"✅ Refactoring complete!  Solution: {Path.Combine(root, name + ".sln")}");
        if (isDryRun) WriteWarning("\n[dry-run] No files were actually created or moved.");
    }

    private static (string Project, string SubFolder) ResolveDestination(ClassifiedFile file, string name)
    {
        if (file.TargetLayer == ProjectLayer.Presentation && (file.FileName == "Program.cs" || file.FileName == "Startup.cs"))
            return ($"{name}.PL", "");

        return file.TargetLayer switch
        {
            ProjectLayer.Domain         => ($"{name}.DAL", "Models"),
            ProjectLayer.Infrastructure => ($"{name}.DAL", "Repositories"),
            ProjectLayer.Application    => ($"{name}.BLL", "Services"),
            ProjectLayer.Presentation   => ($"{name}.PL",  "Controllers"),
            _                           => ($"{name}.BLL", "Common"),
        };
    }

    private static string LayerSuffix(ProjectLayer layer) => layer switch
    {
        ProjectLayer.Domain         => "DAL",
        ProjectLayer.Infrastructure => "DAL",
        ProjectLayer.Application    => "BLL",
        ProjectLayer.Presentation   => "PL",
        _                           => "BLL"
    };

    // AddRef helper removed as it's no longer used

    private void ReportUnclassified(AnalyzedProject project)
    {
        if (!project.UnclassifiedFiles.Any()) return;
        WriteWarning("\n⚠️  Unclassified files (move manually):");
        foreach (var f in project.UnclassifiedFiles)
            WriteWarning($"   • {Path.GetFileName(f)}");
    }
}