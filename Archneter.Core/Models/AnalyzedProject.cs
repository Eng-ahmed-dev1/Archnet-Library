using Archneter.Core.Enums;

namespace Archneter.Core.Models;

/// <summary>
/// Represents the result of analyzing an existing project before refactoring.
/// </summary>
public class AnalyzedProject
{
    /// <summary>
    /// The inferred name of the project (from directory name or .csproj).
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Root directory of the analyzed project.
    /// </summary>
    public string RootDirectory { get; set; } = string.Empty;

    /// <summary>
    /// All classified source files found in the project.
    /// </summary>
    public List<ClassifiedFile> Files { get; set; } = new();

    /// <summary>
    /// Files that could not be automatically classified.
    /// </summary>
    public List<string> UnclassifiedFiles { get; set; } = new();

    /// <summary>
    /// Whether the existing project already has a .sln or .slnx file.
    /// </summary>
    public bool HasSolution { get; set; }

    /// <summary>
    /// Existing .csproj files found.
    /// </summary>
    public List<string> ExistingProjects { get; set; } = new();
}

/// <summary>
/// A single source file with its determined target layer.
/// </summary>
public class ClassifiedFile
{
    /// <summary>
    /// Full path to the source file.
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>
    /// File name only (e.g. "OrderService.cs").
    /// </summary>
    public string FileName => Path.GetFileName(SourcePath);

    /// <summary>
    /// The determined target layer for this file.
    /// </summary>
    public ProjectLayer TargetLayer { get; set; }

    /// <summary>
    /// Confidence level of the classification (High / Medium / Low).
    /// </summary>
    public ClassificationConfidence Confidence { get; set; }

    /// <summary>
    /// The reason why this classification was chosen.
    /// </summary>
    public string ClassificationReason { get; set; } = string.Empty;

    /// <summary>
    /// The original namespace extracted from the file before refactoring.
    /// </summary>
    public string OriginalNamespace { get; set; } = string.Empty;

    /// <summary>
    /// The intended target namespace for this file after refactoring.
    /// </summary>
    public string TargetNamespace { get; set; } = string.Empty;
}