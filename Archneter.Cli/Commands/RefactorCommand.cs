using Archneter.Cli.Attributes;
using Archneter.Cli.Models;
using Archneter.Cli.Services;
using Archneter.Core.Enums;
using Archneter.Core.Models;
using Archneter.Generators.Refactoring;

namespace Archneter.Cli.Commands;

[Command("refactor")]
[Description("Analyze and refactor an existing .NET project into a target architecture")]
[CommandSyntax("refactor --to <architecture> [--dir <path>] [--dry-run] [--skip-backup] [--force]")]
[CommandOption("--to <architecture>",
    "Target architecture (required)",
    "  clean            → Clean Architecture (.Domain / .Application / .Infrastructure / .Api)",
    "  verticalslice    → Vertical Slice (feature-sliced folders inside one Web API)",
    "  modularmonolith  → Modular Monolith (.Host / .Shared / .Modules.{X})",
    "  microservices    → Microservices (.Gateway / .Shared / .Services.{X}.*)",
    "  n-tier           → N-Tier (.DAL / .BLL / .PL)")]
[CommandOption("--dir <path>",
    "Root directory of the project to refactor (default: current directory)")]
[CommandOption("--dry-run",
    "Preview what would happen without touching the disk")]
[CommandOption("--skip-backup",
    "Skip the automatic backup step")]
[CommandOption("--force",
    "Skip the confirmation prompt")]
[CommandOption("--deep-refactor",
    "Optional deep refactoring mode (DI extraction & interface generation using Roslyn)")]
[CommandExample("archneter refactor --to clean")]
[CommandExample("archneter refactor --to verticalslice --dry-run")]
[CommandExample("archneter refactor --to microservices --dir ./MyProject --force")]
public sealed class RefactorCommand : IArchCommand
{
    private readonly ProjectAnalyzer _analyzer;
    private readonly RefactoringStrategyFactory _factory;

    private static readonly Dictionary<string, ArchitectureType> ArchAliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["clean"]           = ArchitectureType.CleanArchitecture,
            ["verticalslice"]   = ArchitectureType.VerticalSlice,
            ["modularmonolith"] = ArchitectureType.ModularMonolith,
            ["microservices"]   = ArchitectureType.Microservices,
            ["n-tier"]          = ArchitectureType.NTier,
            ["ntier"]           = ArchitectureType.NTier,
        };

    public RefactorCommand(ProjectAnalyzer analyzer, RefactoringStrategyFactory factory)
    {
        _analyzer = analyzer;
        _factory  = factory;
    }

    public async Task ExecuteAsync(CommandContext context)
    {
        // ── --to (required) ───────────────────────────────────────────────────────
        if (!context.Options.TryGetValue("--to", out var archAlias) || string.IsNullOrWhiteSpace(archAlias))
        {
            WriteError("Missing required flag: --to <architecture>");
            PrintUsage();
            return;
        }

        if (!ArchAliases.TryGetValue(archAlias, out var targetArch))
        {
            WriteError($"Unknown architecture '{archAlias}'.");
            PrintUsage();
            return;
        }

        // ── --dir ─────────────────────────────────────────────────────────────────
        var dir = context.Options.GetValueOrDefault("--dir", Directory.GetCurrentDirectory());
        if (!Directory.Exists(dir))
        {
            WriteError($"Directory not found: {dir}");
            return;
        }

        // ── Flags ─────────────────────────────────────────────────────────────────
        var isDryRun     = context.Flags.Contains("--dry-run")     || context.Options.ContainsKey("--dry-run");
        var skipBackup   = context.Flags.Contains("--skip-backup") || context.Options.ContainsKey("--skip-backup");
        var force        = context.Flags.Contains("--force")       || context.Options.ContainsKey("--force");
        var deepRefactor = context.Flags.Contains("--deep-refactor") || context.Options.ContainsKey("--deep-refactor");

        // ── Analyze ───────────────────────────────────────────────────────────────
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("🔍 Analyzing project...");
        Console.ResetColor();

        var analyzed = _analyzer.Analyze(dir);
        PrintAnalysisReport(analyzed);

        // ── Confirmation ──────────────────────────────────────────────────────────
        if (!force && !isDryRun)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("\n⚠️  This will restructure your project. A backup will be created first. Continue? [y/N] ");
            Console.ResetColor();

            var answer = Console.ReadLine()?.Trim().ToLower();
            if (answer is not ("y" or "yes"))
            {
                Console.WriteLine("Aborted.");
                return;
            }
        }

        // ── Execute ───────────────────────────────────────────────────────────────
        var options = new RefactorOptions
        {
            ProjectDirectory   = dir,
            TargetArchitecture = targetArch,
            ProjectName        = analyzed.ProjectName,
            DryRun             = isDryRun,
            SkipBackup         = skipBackup,
            Force              = force,
            DeepRefactor       = deepRefactor,
        };

        var strategy = _factory.Create(targetArch, isDryRun);
        await strategy.ExecuteAsync(options, analyzed);
    }

    // ─────────────────────────────────────────────────────────────────────────────

    private static void PrintAnalysisReport(AnalyzedProject project)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"  Project   : {project.ProjectName}");
        Console.WriteLine($"  Directory : {project.RootDirectory}");
        Console.WriteLine($"  Solution  : {(project.HasSolution ? "Yes" : "No")}");
        Console.WriteLine($"  .csproj   : {project.ExistingProjects.Count}");
        Console.ResetColor();
        Console.WriteLine();

        foreach (var group in project.Files.GroupBy(f => f.TargetLayer).OrderBy(g => g.Key.ToString()))
        {
            Console.ForegroundColor = LayerColor(group.Key);
            Console.WriteLine($"  ── {group.Key} ({group.Count()} files)");
            Console.ResetColor();
            foreach (var file in group)
            {
                var dot = file.Confidence == ClassificationConfidence.High   ? "●" :
                          file.Confidence == ClassificationConfidence.Medium ? "◑" : "○";
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"     {dot} {file.FileName,-45} {file.ClassificationReason}");
                Console.ResetColor();
            }
        }

        if (project.UnclassifiedFiles.Any())
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n  ── Unclassified ({project.UnclassifiedFiles.Count} files — will be left in place)");
            Console.ResetColor();
        }
    }

    private static ConsoleColor LayerColor(ProjectLayer layer) => layer switch
    {
        ProjectLayer.Domain         => ConsoleColor.Magenta,
        ProjectLayer.Application    => ConsoleColor.Blue,
        ProjectLayer.Infrastructure => ConsoleColor.DarkYellow,
        ProjectLayer.Presentation   => ConsoleColor.Green,
        ProjectLayer.Tests          => ConsoleColor.DarkCyan,
        _                           => ConsoleColor.Gray,
    };

    private static void PrintUsage()
    {
        Console.WriteLine("\n  Usage:   archneter refactor --to <architecture> [options]");
        Console.WriteLine("  Values:  clean | verticalslice | modularmonolith | microservices | n-tier");
    }

    private static void WriteError(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ {msg}");
        Console.ResetColor();
    }
}