using Archneter.Core.Models;
using Archneter.Generators.Infrastructure;
using System.Text.RegularExpressions;

namespace Archneter.Generators.Refactoring.Strategies;

public class ToApiStrategy : BaseRefactoringStrategy
{
    public ToApiStrategy(ICliService cli) : base(cli) { }

    public override async Task ExecuteAsync(RefactorOptions options, AnalyzedProject analyzedProject)
    {
        var root = options.ProjectDirectory;
        var isDryRun = options.DryRun;
        IsDryRun = isDryRun;

        WriteHeader("Refactoring → Pure Web API");

        if (!options.SkipBackup)
            CreateBackup(root, isDryRun);

        // 1. Cleanup
        WriteHeader("Cleaning up MVC assets");
        var viewsDir = Path.Combine(root, "Views");
        if (Directory.Exists(viewsDir))
        {
            if (!isDryRun) Directory.Delete(viewsDir, true);
            WriteSuccess("  ✔ Deleted Views directory.");
        }
        
        var pagesDir = Path.Combine(root, "Pages");
        if (Directory.Exists(pagesDir))
        {
            if (!isDryRun) Directory.Delete(pagesDir, true);
            WriteSuccess("  ✔ Deleted Pages directory.");
        }

        if (options.RemoveStaticFiles)
        {
            var wwwrootDir = Path.Combine(root, "wwwroot");
            if (Directory.Exists(wwwrootDir))
            {
                if (!isDryRun) Directory.Delete(wwwrootDir, true);
                WriteSuccess("  ✔ Deleted wwwroot directory.");
            }
        }

        // 2. Controllers
        WriteHeader("Updating Controllers");
        var warnings = new List<string>();
        int updatedControllers = 0;

        var controllerFiles = analyzedProject.Files.Where(f => f.TargetLayer == ProjectLayer.Presentation && f.FileName.EndsWith("Controller.cs")).ToList();
        
        foreach (var file in controllerFiles)
        {
            if (isDryRun) continue;

            var content = File.ReadAllText(file.SourcePath);
            bool modified = false;

            // Change base class
            if (content.Contains(": Controller") && !content.Contains(": ControllerBase"))
            {
                content = Regex.Replace(content, @":\s*Controller\b", ": ControllerBase");
                modified = true;
            }

            // Add [ApiController] and [Route] (Idempotent)
            var classMatch = Regex.Match(content, @"public\s+(sealed\s+|partial\s+)?class\s+(\w+Controller)");
            if (classMatch.Success)
            {
                var className = classMatch.Groups[2].Value;
                var classIndex = classMatch.Index;
                
                var prefix = content.Substring(0, classIndex);
                string newAttributes = "";
                
                if (!prefix.Contains("[ApiController]"))
                    newAttributes += "[ApiController]\n    ";
                
                if (!Regex.IsMatch(prefix, @"\[Route\("))
                    newAttributes += "[Route(\"api/[controller]\")]\n    ";
                
                if (newAttributes != "")
                {
                    content = content.Insert(classIndex, newAttributes);
                    modified = true;
                }

                // Check for warnings
                if (content.Contains("return View(") || content.Contains("return View()"))
                    warnings.Add($"{className} -> return View()");
                
                if (content.Contains("return PartialView"))
                    warnings.Add($"{className} -> return PartialView()");
                
                if (content.Contains("return RedirectToAction"))
                    warnings.Add($"{className} -> return RedirectToAction()");
            }

            if (modified)
            {
                File.WriteAllText(file.SourcePath, content);
                updatedControllers++;
            }
        }
        WriteSuccess($"  ✔ Updated {updatedControllers} controllers.");

        // 3. Program.cs / Startup.cs
        WriteHeader("Updating Startup/Program");
        var programFile = analyzedProject.Files.FirstOrDefault(f => f.FileName == "Program.cs" || f.FileName == "Startup.cs");
        if (programFile != null && !isDryRun)
        {
            var content = File.ReadAllText(programFile.SourcePath);
            
            // Remove MVC specific configurations
            content = content.Replace("AddControllersWithViews()", "AddControllers()");
            content = content.Replace("AddControllersWithViews(options =>", "AddControllers(options =>");
            content = Regex.Replace(content, @"\.AddRazorPages\([^)]*\)\s*;", "");
            content = Regex.Replace(content, @"builder\.Services\.AddRazorPages\([^)]*\)\s*;", "");
            content = content.Replace(".AddRazorRuntimeCompilation()", "");
            
            if (options.RemoveStaticFiles)
            {
                content = content.Replace("app.UseStaticFiles();", "");
            }

            content = Regex.Replace(content, @"app\.MapControllerRoute\([^)]+\);", "app.MapControllers();");
            content = Regex.Replace(content, @"app\.MapRazorPages\(\);", "");
            
            // Swagger setup (Idempotent)
            if (!content.Contains("AddSwaggerGen"))
            {
                if (content.Contains("builder.Services.AddControllers"))
                {
                    content = Regex.Replace(content, @"(builder\.Services\.AddControllers[^\n]*\n)", "$1builder.Services.AddEndpointsApiExplorer();\nbuilder.Services.AddSwaggerGen();\n");
                }
                else if (content.Contains("services.AddControllers"))
                {
                    content = Regex.Replace(content, @"(services\.AddControllers[^\n]*\n)", "$1        services.AddEndpointsApiExplorer();\n        services.AddSwaggerGen();\n");
                }
            }
            
            if (!content.Contains("UseSwagger"))
            {
                if (content.Contains("app.UseAuthorization()"))
                {
                    content = Regex.Replace(content, @"(app\.UseAuthorization\(\);\s*\n)", "$1\napp.UseSwagger();\napp.UseSwaggerUI();\n");
                }
                else if (content.Contains("app.UseRouting()"))
                {
                    content = Regex.Replace(content, @"(app\.UseRouting\(\);\s*\n)", "$1\n            app.UseSwagger();\n            app.UseSwaggerUI();\n");
                }
            }

            File.WriteAllText(programFile.SourcePath, content);
            WriteSuccess($"  ✔ Updated {programFile.FileName}.");
        }

        // 4. Packages
        WriteHeader("Configuring Packages");
        var mainProject = analyzedProject.ExistingProjects.FirstOrDefault(p => p.Contains(".Api") || p.Contains(".Web") || p.Equals(analyzedProject.ExistingProjects.First()));
        if (mainProject != null && !isDryRun)
        {
            await Cli.AddPackageAsync(mainProject, "Swashbuckle.AspNetCore", "7.*");
            WriteSuccess("  ✔ Swashbuckle.AspNetCore configured.");
        }

        // 5. Report
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("MVC → API Refactor Report");
        Console.WriteLine("-------------------------");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✓ Views/Pages removed (if existed)");
        Console.WriteLine($"✓ {updatedControllers} Controllers updated");
        Console.WriteLine("✓ Swagger configured");
        Console.WriteLine("✓ Entry point updated");
        
        if (warnings.Any())
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warnings:");
            var distinctWarnings = warnings.Distinct().ToList();
            foreach(var w in distinctWarnings)
                Console.WriteLine($"- {w}");
            
            Console.WriteLine();
            Console.WriteLine($"Manual changes required: {distinctWarnings.Count}");
        }
        Console.ResetColor();
        Console.WriteLine();
    }
}
