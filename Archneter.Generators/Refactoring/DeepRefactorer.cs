using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Archneter.Generators.Refactoring;

public static class DeepRefactorer
{
    public static void Apply(IEnumerable<Core.Models.ClassifiedFile> movedFiles)
    {
        var filesList = movedFiles.Where(f => !string.IsNullOrEmpty(f.SourcePath) && File.Exists(f.SourcePath)).ToList();
        
        // 1. Collect all class names to detect concrete dependencies
        var concreteClasses = new HashSet<string>();
        foreach (var file in filesList)
        {
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file.SourcePath));
            var root = tree.GetRoot();
            var classDecls = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var c in classDecls)
            {
                concreteClasses.Add(c.Identifier.Text);
                System.Console.WriteLine($"Found concrete class: {c.Identifier.Text} in {file.SourcePath}");
            }
        }

        var interfacesToGenerate = new Dictionary<string, (string SourceFilePath, string InterfaceName)>();

        // 2. Rewrite fields and constructors
        foreach (var file in filesList)
        {
            var content = File.ReadAllText(file.SourcePath);
            var tree = CSharpSyntaxTree.ParseText(content);
            var root = tree.GetRoot();

            var rewriter = new ConstructorInjectionRewriter(concreteClasses);
            var newRoot = rewriter.Visit(root);

            if (newRoot != root)
            {
                File.WriteAllText(file.SourcePath, newRoot.ToFullString());
                
                // Keep track of extracted dependencies to generate interfaces
                foreach (var dep in rewriter.ExtractedDependencies)
                {
                    if (!interfacesToGenerate.ContainsKey(dep))
                    {
                        var depFile = filesList.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f.SourcePath) == dep);
                        if (depFile != null)
                        {
                            interfacesToGenerate[dep] = (depFile.SourcePath, $"I{dep}");
                        }
                    }
                }
            }
        }

        // 3. Generate interfaces and update concrete classes
        foreach (var kvp in interfacesToGenerate)
        {
            var concreteName = kvp.Key;
            var sourcePath = kvp.Value.SourceFilePath;
            var interfaceName = kvp.Value.InterfaceName;

            var content = File.ReadAllText(sourcePath);
            var tree = CSharpSyntaxTree.ParseText(content);
            var root = tree.GetRoot();

            var interfaceGenerator = new InterfaceImplementerRewriter(concreteName, interfaceName);
            var newRoot = interfaceGenerator.Visit(root);

            if (newRoot != root)
            {
                File.WriteAllText(sourcePath, newRoot.ToFullString());
                
                // Create the interface file in the same directory
                var dir = Path.GetDirectoryName(sourcePath);
                var interfacePath = Path.Combine(dir!, $"{interfaceName}.cs");
                
                // Extract namespace
                var nsDecl = newRoot.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
                var ns = nsDecl?.Name.ToString() ?? "GlobalNamespace";

                var interfaceContent = $@"namespace {ns};

public interface {interfaceName}
{{
}}
";
                if (!File.Exists(interfacePath))
                {
                    File.WriteAllText(interfacePath, interfaceContent);
                }
            }
        }
    }

    private class ConstructorInjectionRewriter : CSharpSyntaxRewriter
    {
        private readonly HashSet<string> _knownClasses;
        public HashSet<string> ExtractedDependencies { get; } = new();

        public ConstructorInjectionRewriter(HashSet<string> knownClasses)
        {
            _knownClasses = knownClasses;
        }

        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var fieldsToInject = new List<(VariableDeclaratorSyntax Declarator, string TypeName, string InterfaceName)>();

            // Find fields initialized with new Type()
            var newMembers = new List<MemberDeclarationSyntax>();
            foreach (var member in node.Members)
            {
                if (member is FieldDeclarationSyntax field)
                {
                    var typeName = field.Declaration.Type.ToString();
                    var declarator = field.Declaration.Variables.FirstOrDefault();

                    if (declarator?.Initializer?.Value is BaseObjectCreationExpressionSyntax creation)
                    {
                        string actualType = "";
                        if (creation is ObjectCreationExpressionSyntax explicitCreation)
                        {
                            if (explicitCreation.Type.ToString() == typeName)
                                actualType = typeName;
                        }
                        else if (creation is ImplicitObjectCreationExpressionSyntax)
                        {
                            actualType = typeName;
                        }

                        if (!string.IsNullOrEmpty(actualType) && _knownClasses.Contains(actualType))
                        {
                            System.Console.WriteLine($"DeepRefactorer: Replacing new {actualType}() with {actualType} injection.");
                            var interfaceName = $"I{actualType}";
                            ExtractedDependencies.Add(actualType);

                            // Remove initializer and change type to interface
                            var newDeclarator = declarator.WithInitializer(null);
                            var newType = SyntaxFactory.ParseTypeName(interfaceName).WithTriviaFrom(field.Declaration.Type);
                            
                            var newDeclaration = field.Declaration.ReplaceNode(declarator, newDeclarator).WithType(newType);
                            var newField = field.WithDeclaration(newDeclaration);
                            
                            newMembers.Add(newField);
                            fieldsToInject.Add((newDeclarator, actualType, interfaceName));
                            continue;
                        }
                    }
                }
                newMembers.Add(member);
            }

            if (!fieldsToInject.Any())
                return base.VisitClassDeclaration(node);

            // Create or update constructor
            var constructor = newMembers.OfType<ConstructorDeclarationSyntax>().FirstOrDefault();
            var assignments = fieldsToInject.Select(f => 
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(f.Declarator.Identifier.Text),
                        SyntaxFactory.IdentifierName(f.Declarator.Identifier.Text.TrimStart('_').ToLower())
                    )
                )
            ).ToList();

            var parameters = fieldsToInject.Select(f => 
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(f.Declarator.Identifier.Text.TrimStart('_').ToLower()))
                             .WithType(SyntaxFactory.ParseTypeName(f.InterfaceName))
            ).ToArray();

            if (constructor != null)
            {
                var newConstructor = constructor.AddParameterListParameters(parameters)
                                                .AddBodyStatements(assignments.ToArray());
                newMembers[newMembers.IndexOf(constructor)] = newConstructor;
            }
            else
            {
                var newConstructor = SyntaxFactory.ConstructorDeclaration(node.Identifier)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddParameterListParameters(parameters)
                    .WithBody(SyntaxFactory.Block(assignments))
                    .NormalizeWhitespace()
                    .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
                
                // Insert after fields
                var lastFieldIndex = newMembers.FindLastIndex(m => m is FieldDeclarationSyntax);
                newMembers.Insert(lastFieldIndex + 1, newConstructor);
            }

            return node.WithMembers(SyntaxFactory.List(newMembers));
        }
    }

    private class InterfaceImplementerRewriter : CSharpSyntaxRewriter
    {
        private readonly string _className;
        private readonly string _interfaceName;

        public InterfaceImplementerRewriter(string className, string interfaceName)
        {
            _className = className;
            _interfaceName = interfaceName;
        }

        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (node.Identifier.Text == _className)
            {
                var baseList = node.BaseList ?? SyntaxFactory.BaseList();
                var hasInterface = baseList.Types.Any(t => t.Type.ToString() == _interfaceName);
                
                if (!hasInterface)
                {
                    var newType = SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(_interfaceName));
                    baseList = baseList.AddTypes(newType);
                    return node.WithBaseList(baseList);
                }
            }
            return base.VisitClassDeclaration(node);
        }
    }
}
