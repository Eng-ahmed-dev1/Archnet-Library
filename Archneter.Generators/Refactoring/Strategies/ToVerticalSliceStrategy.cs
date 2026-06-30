using Archneter.Core.Models;
using Archneter.Generators.Infrastructure;

namespace Archneter.Generators.Refactoring.Strategies;

/// <summary>
/// Refactors an existing project into a Vertical Slice architecture:
///   ProjectName.Api/
///     Features/
///       {FeatureName}/
///         Commands/
///         Queries/
///         DTOs/
///         Endpoints/
/// Files are grouped into feature slices inferred from their names.
/// </summary>
public class ToVerticalSliceStrategy : BaseRefactoringStrategy
{
    public ToVerticalSliceStrategy(ICliService cli) : base(cli) { }
    public override async Task ExecuteAsync(RefactorOptions options, AnalyzedProject analyzedProject)
    {
        var name     = analyzedProject.ProjectName;
        var root     = options.ProjectDirectory;
        var isDryRun = options.DryRun;
        IsDryRun = isDryRun;

        WriteHeader($"Refactoring → Vertical Slice  [{name}]");

        if (!options.SkipBackup)
            CreateBackup(root, isDryRun);

        // ── Single Web API project ────────────────────────────────────────
        WriteHeader("Creating project");
        await RunDotnetAsync(root, "new", "webapi", "-n", $"{name}.Api", "--force");

        if (!analyzedProject.HasSolution)
            await RunDotnetAsync(root, "new", "sln", "-n", name);

        var slnPath = isDryRun
            ? Path.Combine(root, $"{name}.sln")
            : Directory.GetFiles(root, $"{name}.sln*").FirstOrDefault() ?? Path.Combine(root, $"{name}.sln");

        await Cli.AddMultipleToSolutionAsync(slnPath, new[] { Path.Combine(root, $"{name}.Api", $"{name}.Api.csproj") });

        // ── Create feature folders ────────────────────────────────────────
        WriteHeader("Creating feature slices");

        var features = InferFeatures(analyzedProject.Files);
        foreach (var feature in features)
        {
            foreach (var sub in new[] { "Commands", "Queries", "DTOs", "Endpoints" })
                EnsureDirectory(Path.Combine(root, $"{name}.Api", "Features", feature, sub));
        }

        // ── Move files ────────────────────────────────────────────────────
        WriteHeader("Moving files");

        foreach (var file in analyzedProject.Files)
        {
            var feature   = InferFeature(file.FileName, features);
            var subFolder = ResolveSubFolder(file);

            string destPath, newNs;
            if (file.TargetLayer == ProjectLayer.Presentation && (file.FileName == "Program.cs" || file.FileName == "Startup.cs"))
            {
                destPath = Path.Combine(root, $"{name}.Api", file.FileName);
                newNs = $"{name}.Api";
            }
            else
            {
                destPath = Path.Combine(root, $"{name}.Api", "Features", feature, subFolder, file.FileName);
                newNs = NamespaceRewriter.BuildNamespace(name, $"Api.Features.{feature}", subFolder);
            }

            file.TargetNamespace = newNs;
            file.SourcePath = MoveFile(file.SourcePath, destPath, name, newNs);
        }

        RewriteNamespaces(analyzedProject.Files);

        if (options.DeepRefactor)
        {
            WriteHeader("Applying Deep Refactoring");
            ApplyDeepRefactoring(analyzedProject.Files);
        }

        PropagatePackageReferences(analyzedProject, Path.Combine(root, $"{name}.Api", $"{name}.Api.csproj"));

        if (!isDryRun)
        {
            WriteHeader("Installing Main Packages");
            await PackageInstaller.AddApplicationPackagesAsync(Cli, Path.Combine(root, $"{name}.Api", $"{name}.Api.csproj"));
            await PackageInstaller.AddInfrastructurePackagesAsync(Cli, Path.Combine(root, $"{name}.Api", $"{name}.Api.csproj"), Archneter.Core.Enums.DatabaseType.SqlServer);
        }

        ReportUnclassified(analyzedProject);

        WriteHeader("Done");
        WriteSuccess($"✅ Refactoring complete → Vertical Slice!  ({features.Count} features detected)");
        if (isDryRun) WriteWarning("\n[dry-run] No files were actually created or moved.");
    }

    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Infers feature names by extracting the domain noun from each file name.
    /// e.g. "OrderService.cs", "OrderController.cs", "GetOrderQuery.cs" → "Orders"
    /// </summary>
    private static HashSet<string> InferFeatures(List<ClassifiedFile> files)
    {
        var suffixes = new[]
        {
            "Controller", "Service", "Repository", "Handler",
            "Command", "Query", "Validator", "Dto", "Mapping",
            "Entity", "Model", "Request", "Response", "Profile"
        };

        var features = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            var name = Path.GetFileNameWithoutExtension(file.FileName);
            foreach (var suffix in suffixes)
            {
                if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    var domain = name[..^suffix.Length].Trim();
                    // Strip leading Get/Create/Update/Delete/Add verbs
                    domain = StripVerb(domain);
                    if (!string.IsNullOrWhiteSpace(domain))
                    {
                        features.Add(Pluralize(domain));
                        break;
                    }
                }
            }
        }

        return features.Count > 0 ? features : new HashSet<string> { "Core" };
    }

    private static string InferFeature(string fileName, HashSet<string> features)
    {
        foreach (var feature in features)
        {
            var singular = feature.TrimEnd('s');
            if (fileName.Contains(singular, StringComparison.OrdinalIgnoreCase) ||
                fileName.Contains(feature,  StringComparison.OrdinalIgnoreCase))
                return feature;
        }
        return features.First(); // fallback to first feature
    }

    private static string ResolveSubFolder(ClassifiedFile file) => file.TargetLayer switch
    {
        ProjectLayer.Application  when file.FileName.Contains("Command",  StringComparison.OrdinalIgnoreCase) => "Commands",
        ProjectLayer.Application  when file.FileName.Contains("Query",    StringComparison.OrdinalIgnoreCase) => "Queries",
        ProjectLayer.Application  when file.FileName.Contains("Dto",      StringComparison.OrdinalIgnoreCase) => "DTOs",
        ProjectLayer.Presentation => "Endpoints",
        ProjectLayer.Domain       => "DTOs",
        _                         => "Commands",
    };

    private static string StripVerb(string name)
    {
        var verbs = new[] { "Get", "Create", "Update", "Delete", "Add", "Remove", "List", "Find", "Fetch" };
        foreach (var verb in verbs)
            if (name.StartsWith(verb, StringComparison.OrdinalIgnoreCase))
                return name[verb.Length..];
        return name;
    }

    private static string Pluralize(string word) =>
        word.EndsWith('s') ? word : word + "s";

    private void ReportUnclassified(AnalyzedProject project)
    {
        if (!project.UnclassifiedFiles.Any()) return;
        WriteWarning("\n⚠️  Unclassified files (move manually):");
        foreach (var f in project.UnclassifiedFiles)
            WriteWarning($"   • {Path.GetFileName(f)}");
    }
}