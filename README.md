# Archneter

Archneter is a lightweight, extensible command-line tool (CLI) designed to automate the scaffolding and creation of various software architecture templates in .NET. It streamlines project setup by generating clean folder structures, creating projects, linking them within a solution, and configuring references automatically using the .NET CLI.

---

## Project Structure

The codebase is organized into three primary projects grouped under a single .NET solution (`archneter.slnx`):

```
Archneter/
├── Archneter.Core/          # Domain abstractions, models, and shared enums
├── Archneter.Generators/    # Architecture-specific code generation logic
└── Archneter.Cli/           # Command-Line Interface and argument dispatching
```

---

## Detailed Directory & File Breakdown

### 1. [Archneter.Core](./Archneter.Core)
This project defines the core abstractions, shared models, and configurations used across the solution. It does not depend on any other projects.

*   **`Abstractions/`**
    *   **[`IArchitectureGenerator.cs`](./Archneter.Core/Abstractions/IArchitectureGenerator.cs)**: Defines the primary interface for all architecture generators. It exposes `Task GenerateAsync(ProjectOptions options)`.
*   **`Enums/`**
    *   **[`ArchitectureType.cs`](./Archneter.Core/Enums/ArchitectureType.cs)**: An enum representing the supported architecture styles:
        *   `CleanArchitecture` (1)
        *   `VerticalSlice` (2)
        *   `ModularMonolith` (3)
        *   `Microservices` (4)
*   **`Models/`**
    *   **[`ProjectOptions.cs`](./Archneter.Core/Models/ProjectOptions.cs)**: Data class holding configuration parameters specified by the user (e.g., `ProjectName`, selected `ArchitectureType`, and a flag to `GenerateTests`).

---

### 2. [Archneter.Generators](./Archneter.Generators)
This project implements the architecture generators. It depends on `Archneter.Core` and uses the underlying system's `dotnet` CLI to perform project bootstrapping.

*   **`CleanArchitecture/`**
    *   **[`CleanArchitectureGenerator.cs`](./Archneter.Generators/CleanArchitecture/CleanArchitectureGenerator.cs)**: The concrete implementation of `IArchitectureGenerator` for Clean Architecture. Scaffolds a 4-layer architecture:
        *   `.Domain` (Class Library)
        *   `.Application` (Class Library, depends on `.Domain`)
        *   `.Infrastructure` (Class Library, depends on `.Application`)
        *   `.Api` (Web API, depends on `.Application` and `.Infrastructure`)
        *   *Optional:* Scaffolds `.Unit.Tests` and `.Integration.Tests` (xUnit projects) if `GenerateTests` is set to `true`.
*   **`Infrastructure/`**
    *   **[`DotnetCliService.cs`](./Archneter.Generators/Infrastructure/DotnetCliService.cs)**: A utility service wrapper that runs command-line processes. It provides helper methods to interact with the installed .NET SDK:
        *   `RunAsync()`: Spawns and manages a `dotnet` process.
        *   `CreateProjectAsync()`: Executes `dotnet new <template>`.
        *   `AddToSolutionAsync()`: Executes `dotnet sln <sln> add <project>`.
        *   `AddReferenceAsync()`: Executes `dotnet add <project> reference <dependency>`.
*   **Placeholder Directories** (for future implementations):
    *   `Microservices/`
    *   `ModularMonolith/`
    *   `VerticalSlice/`

---

### 3. [Archneter.Cli](./Archneter.Cli)
The entry point of the CLI application. It handles parsing command-line parameters, matching them to commands, and executing actions.

*   **[`Program.cs`](./Archneter.Cli/Program.cs)**: The entry point. Discovers and runs commands by querying the `CommandRegistry` and using the `CommandDispatcher`.
*   **`Attributes/`**
    *   **[`CommandAttribute.cs`](./Archneter.Cli/Attributes/CommandAttribute.cs)**: Specifies the command keyword (e.g., `[Command("new")]`).
    *   **[`DescriptionAttribute.cs`](./Archneter.Cli/Attributes/DescriptionAttribute.cs)**: Provides a short description of the command.
    *   **[`CommandSyntaxAttribute.cs`](./Archneter.Cli/Attributes/CommandSyntaxAttribute.cs)**: Defines the execution syntax for help generation.
    *   **[`CommandOptionAttribute.cs`](./Archneter.Cli/Attributes/CommandOptionAttribute.cs)**: Declares option templates, descriptions, and detail lists.
    *   **[`CommandExampleAttribute.cs`](./Archneter.Cli/Attributes/CommandExampleAttribute.cs)**: Provides concrete examples of command usage.
*   **`Commands/`**
    *   **[`IArchCommand.cs`](./Archneter.Cli/Commands/IArchCommand.cs)**: Interface defining standard executable commands with `Task ExecuteAsync(CommandContext context)`.
    *   **[`NewCommand.cs`](./Archneter.Cli/Commands/NewCommand.cs)**: Implements the `new` command. Parses the project name, selected architecture flags, and test flags, then fires the appropriate generator from `GeneratorFactory`.
    *   **[`HelpCommand.cs`](./Archneter.Cli/Commands/HelpCommand.cs)**: Displays dynamic and formatted usage instructions, command lists, options, and examples retrieved from the registry.
*   **`Models/`**
    *   **[`CommandContext.cs`](./Archneter.Cli/Models/CommandContext.cs)**: Contains parsed arguments, flags, and key-value options for execution context.
    *   **[`CommandDescriptor.cs`](./Archneter.Cli/Models/CommandDescriptor.cs)**: Couples a command name with its `IArchCommand` instance.
    *   **[`OptionMetadata.cs`](./Archneter.Cli/Models/OptionMetadata.cs)**: Holds structural metadata of a command option.
    *   **[`CommandMetadata.cs`](./Archneter.Cli/Models/CommandMetadata.cs)**: Holds aggregated command metadata including syntax, options, and examples.
*   **`Parsing/`**
    *   **[`ArgumentParser.cs`](./Archneter.Cli/Parsing/ArgumentParser.cs)**: Simple parser that maps console arguments (e.g., command, project name, key-value option pairs like `--arch clean`) into a structured `CommandContext`.
*   **`Services/`**
    *   **[`CommandRegistry.cs`](./Archneter.Cli/Services/CommandRegistry.cs)**: Discovers and manages CLI commands and their metadata dynamically using reflection.
    *   **[`CliConsoleWriter.cs`](./Archneter.Cli/Services/CliConsoleWriter.cs)**: A simple formatting utility to write aligned headers, columns, and list items to the console.
    *   **[`CommandDispatcher.cs`](./Archneter.Cli/Services/CommandDispatcher.cs)**: Selects and runs the correct command depending on parsed arguments.
    *   **[`GeneratorFactory.cs`](./Archneter.Cli/Services/GeneratorFactory.cs)**: Resolves the appropriate implementation of `IArchitectureGenerator` based on the specified `ArchitectureType`.
    *   **[`ProjectWizardService.cs`](./Archneter.Cli/Services/ProjectWizardService.cs)**: An interactive command-line wizard (currently placeholder) to prompt the user step-by-step for project generation inputs.

---

## How to Get Started

### Prerequisites
Make sure you have the following installed on your machine:
*   [.NET 8.0 SDK](https://dotnet.microsoft.com/download) (or higher)

### Build the Tool
To build the solution, run:
```bash
dotnet build archneter.slnx
```

### Run the CLI
You can execute the CLI project directly using `dotnet run`:
```bash
dotnet run --project Archneter.Cli/Archneter.Cli.csproj -- [command] [arguments] [options]
```

---

## Usage Guide

### Display Help
To see all commands and configurations, run the `help` command:
```bash
dotnet run --project Archneter.Cli/Archneter.Cli.csproj -- help
```

### Create a New Project
Use the `new` command to generate a template solution.

```bash
dotnet run --project Archneter.Cli/Archneter.Cli.csproj -- new <ProjectName> [options]
```

#### Options:
*   `--arch <type>`: Specifies the architecture template. Supported value:
    *   `clean` (Clean Architecture - Default)
*   `--tests <true|false>`: Scaffolds accompanying unit and integration test projects. (Default: `false`)

#### Examples:

1.  **Generate a standard Clean Architecture solution:**
    ```bash
    dotnet run --project Archneter.Cli/Archneter.Cli.csproj -- new InventorySystem --arch clean
    ```

2.  **Generate a Clean Architecture solution with Unit and Integration tests:**
    ```bash
    dotnet run --project Archneter.Cli/Archneter.Cli.csproj -- new ECommerceSystem --arch clean --tests true
    ```
