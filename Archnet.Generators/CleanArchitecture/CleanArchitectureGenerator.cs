using Archnet.Core.Abstractions;
using Archnet.Core.Models;
using Archnet.Generators.Infrastructure;

namespace Archnet.Generators.CleanArchitecture;

public class CleanArchitectureGenerator : IArchitectureGenerator
{
    public async Task GenerateAsync(ProjectOptions options)
    {
        var name = options.ProjectName;
        var rootPath = Path.Combine(Directory.GetCurrentDirectory(), name);
        var srcPath = Path.Combine(rootPath, "src");
        var testsPath = Path.Combine(rootPath, "tests");

        Directory.CreateDirectory(rootPath);
        Directory.CreateDirectory(srcPath);

        await DotnetCliService.RunAsync($"new sln -n {name}", rootPath);
        var slnPath = Path.Combine(rootPath, $"{name}.sln");

        var domainPath = Path.Combine(srcPath, $"{name}.Domain");
        var applicationPath = Path.Combine(srcPath, $"{name}.Application");
        var infrastructurePath = Path.Combine(srcPath, $"{name}.Infrastructure");
        var apiPath = Path.Combine(srcPath, $"{name}.Api");

        await DotnetCliService.CreateProjectAsync("classlib", $"{name}.Domain", domainPath);
        await DotnetCliService.CreateProjectAsync("classlib", $"{name}.Application", applicationPath);
        await DotnetCliService.CreateProjectAsync("classlib", $"{name}.Infrastructure", infrastructurePath);
        await DotnetCliService.CreateProjectAsync("webapi", $"{name}.Api", apiPath);

        await DotnetCliService.AddToSolutionAsync(slnPath, $"{domainPath}/{name}.Domain.csproj");
        await DotnetCliService.AddToSolutionAsync(slnPath, $"{applicationPath}/{name}.Application.csproj");
        await DotnetCliService.AddToSolutionAsync(slnPath, $"{infrastructurePath}/{name}.Infrastructure.csproj");
        await DotnetCliService.AddToSolutionAsync(slnPath, $"{apiPath}/{name}.Api.csproj");

        await DotnetCliService.AddReferenceAsync(
            $"{applicationPath}/{name}.Application.csproj",
            $"{domainPath}/{name}.Domain.csproj");

        await DotnetCliService.AddReferenceAsync(
            $"{infrastructurePath}/{name}.Infrastructure.csproj",
            $"{applicationPath}/{name}.Application.csproj");

        await DotnetCliService.AddReferenceAsync(
            $"{apiPath}/{name}.Api.csproj",
            $"{applicationPath}/{name}.Application.csproj");

        await DotnetCliService.AddReferenceAsync(
            $"{apiPath}/{name}.Api.csproj",
            $"{infrastructurePath}/{name}.Infrastructure.csproj");

        Directory.CreateDirectory(Path.Combine(applicationPath, "Common"));
        Directory.CreateDirectory(Path.Combine(applicationPath, "DependencyInjection"));
        Directory.CreateDirectory(Path.Combine(applicationPath, "DTOs"));
        Directory.CreateDirectory(Path.Combine(applicationPath, "UseCases"));

        if (options.GenerateTests)
        {
            Directory.CreateDirectory(testsPath);

            var domainTestsPath = Path.Combine(testsPath, $"{name}.Domain.Tests");
            await DotnetCliService.CreateProjectAsync("xunit", $"{name}.Domain.Tests", domainTestsPath);
            await DotnetCliService.AddToSolutionAsync(slnPath, $"{domainTestsPath}/{name}.Domain.Tests.csproj");
            await DotnetCliService.AddReferenceAsync(
                $"{domainTestsPath}/{name}.Domain.Tests.csproj",
                $"{domainPath}/{name}.Domain.csproj");
        }

        Console.WriteLine($"Clean Architecture solution '{name}' generated successfully.");
    }
}