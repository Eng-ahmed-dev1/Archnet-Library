using System.Text.RegularExpressions;

namespace Archneter.Generators.Refactoring;

/// <summary>
/// Rewrites the namespace declaration and using directives inside a .cs file
/// to reflect its new location in the refactored project structure.
/// </summary>
public static class NamespaceRewriter
{
    // Matches:  namespace Foo.Bar;          (file-scoped)
    //           namespace Foo.Bar {         (block-scoped)
    private static readonly Regex NamespaceDeclarationRegex =
        new(@"^(?<indent>\s*)namespace\s+(?<ns>[\w.]+)(?<tail>\s*[;{])", RegexOptions.Multiline);

    // Matches:  using Foo.Bar;
    private static readonly Regex UsingDirectiveRegex =
        new(@"^(?<indent>\s*)using\s+(?<ns>[\w.]+)\s*;", RegexOptions.Multiline);

    /// <summary>
    /// Rewrites the file at <paramref name="filePath"/> in-place,
    /// replacing any occurrence of <paramref name="oldRootNamespace"/>
    /// with <paramref name="newRootNamespace"/> in both the namespace
    /// declaration and all using directives.
    /// </summary>
    /// <param name="filePath">Absolute path of the .cs file to rewrite.</param>
    /// <param name="oldRootNamespace">
    ///     The original root namespace to replace (e.g. "MyApp").
    ///     Pass the full old namespace if the project had no clear root.
    /// </param>
    /// <param name="newNamespace">
    ///     The exact new namespace this file should declare
    ///     (e.g. "MyApp.Domain.Entities").
    /// </param>
    public static void Rewrite(string filePath, string newNamespace, Dictionary<string, List<string>> namespaceMap)
    {
        if (!File.Exists(filePath)) return;

        var content = File.ReadAllText(filePath);
        var changed = false;

        // ── 1. Replace the namespace declaration ───────────────────────────
        content = NamespaceDeclarationRegex.Replace(content, match =>
        {
            changed = true;
            return $"{match.Groups["indent"].Value}namespace {newNamespace}{match.Groups["tail"].Value}";
        });

        // ── 2. Replace using directives ────────────────────────────────────
        content = UsingDirectiveRegex.Replace(content, match =>
        {
            var ns = match.Groups["ns"].Value;
            if (namespaceMap.TryGetValue(ns, out var targetNamespaces))
            {
                changed = true;
                var indent = match.Groups["indent"].Value;
                // If the namespace is mapped to exactly itself (or just one target that matches the current one) we might skip, but writing it is fine
                var directives = targetNamespaces
                    .Where(n => n != newNamespace) // Don't add 'using' for the namespace the file is currently in
                    .Select(n => $"{indent}using {n};");
                return string.Join(Environment.NewLine, directives);
            }
            return match.Value;
        });

        // Remove empty lines created by removing self-referencing usings
        content = Regex.Replace(content, @"^\s*$\n|\r", string.Empty, RegexOptions.Multiline);

        if (changed)
            File.WriteAllText(filePath, content);
    }

    /// <summary>
    /// Derives the correct namespace for a file given:
    /// - the solution root namespace (e.g. "MyApp")
    /// - the target project suffix   (e.g. "Domain")
    /// - the relative folder path within that project (e.g. "Entities")
    /// </summary>
    public static string BuildNamespace(string rootNamespace, string projectSuffix, string? subFolder = null)
    {
        var parts = new List<string> { rootNamespace, projectSuffix };
        if (!string.IsNullOrWhiteSpace(subFolder))
            parts.Add(subFolder.Replace(Path.DirectorySeparatorChar, '.').Trim('.'));

        return string.Join('.', parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }
}