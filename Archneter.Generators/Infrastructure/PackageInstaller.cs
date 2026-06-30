using Archneter.Core.Enums;

namespace Archneter.Generators.Infrastructure
{
    public static class PackageInstaller
    {
        public static async Task AddApplicationPackagesAsync(ICliService cli, string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath)) return;
            await cli.AddPackageAsync(projectPath, "MediatR");
            await cli.AddPackageAsync(projectPath, "FluentValidation", "12.*");
            await cli.AddPackageAsync(projectPath, "FluentValidation.DependencyInjectionExtensions", "12.*");
            await cli.RestoreProjectAsync(projectPath);
        }

        public static async Task AddInfrastructurePackagesAsync(ICliService cli, string projectPath, DatabaseType dbType)
        {
            if (string.IsNullOrEmpty(projectPath)) return;
            await cli.AddPackageAsync(projectPath, "Microsoft.EntityFrameworkCore", "9.*");
            await cli.AddPackageAsync(projectPath, "Microsoft.EntityFrameworkCore.Tools", "9.*");
            
            switch (dbType)
            {
                case DatabaseType.SqlServer:
                    await cli.AddPackageAsync(projectPath, "Microsoft.EntityFrameworkCore.SqlServer", "9.*");
                    break;
                case DatabaseType.PostgreSQL:
                    await cli.AddPackageAsync(projectPath, "Npgsql.EntityFrameworkCore.PostgreSQL", "9.*");
                    break;
                case DatabaseType.MongoDB:
                    await cli.AddPackageAsync(projectPath, "MongoDB.Driver", "3.*");
                    break;
            }

            await cli.AddPackageAsync(projectPath, "Microsoft.AspNetCore.Identity.EntityFrameworkCore", "9.*");
            await cli.AddPackageAsync(projectPath, "Microsoft.AspNetCore.Authentication.JwtBearer", "9.*");
            await cli.AddPackageAsync(projectPath, "System.IdentityModel.Tokens.Jwt", "8.*");
            await cli.RestoreProjectAsync(projectPath);
        }

        public static async Task AddApiPackagesAsync(ICliService cli, string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath)) return;
            await cli.AddPackageAsync(projectPath, "Microsoft.EntityFrameworkCore.Design", "9.*");
            await cli.AddPackageAsync(projectPath, "Swashbuckle.AspNetCore", "7.*");
            await cli.RestoreProjectAsync(projectPath);
        }
    }
}
