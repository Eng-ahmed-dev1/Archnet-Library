using Archneter.Core.Enums;

namespace Archneter.Core.Models;

/// <summary>
/// Holds the configuration options for the refactor command.
/// </summary>
public class RefactorOptions
{
    /// <summary>
    /// The root directory of the existing project to refactor.
    /// Defaults to the current working directory.
    /// </summary>
    public string ProjectDirectory { get; set; } = Directory.GetCurrentDirectory();

    /// <summary>
    /// The target architecture to refactor into.
    /// </summary>
    public ArchitectureType TargetArchitecture { get; set; }

    /// <summary>
    /// The project/solution name. Inferred from the directory name if not specified.
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// If true, shows what would happen without making any changes to disk.
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// If true, skips the backup step before refactoring.
    /// </summary>
    public bool SkipBackup { get; set; }

    /// <summary>
    /// If true, skips the confirmation prompt before executing.
    /// </summary>
    public bool Force { get; set; }

    /// <summary>
    /// If true, performs Roslyn-based deep refactoring (DI extraction, Interface generation).
    /// </summary>
    public bool DeepRefactor { get; set; }
}