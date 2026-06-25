using Archneter.Cli.Models;

namespace Archneter.Cli.Parsing
{
    public class ArgumentParser
    {
        /// <summary>
        /// Known boolean flags (no value following them).
        /// Everything else starting with '--' that has a value is treated as an option.
        /// </summary>
        private static readonly HashSet<string> _knownFlags = new(StringComparer.OrdinalIgnoreCase)
        {
            "--dry-run",
            "--skip-backup",
            "--force",
            "--deep-refactor"
        };

        public CommandContext Parse(string[] args)
        {
            var context = new CommandContext();

            if (args.Length == 0)
                return context;

            context.Command = args[0];

            if (args.Length == 1)
                return context;

            // Second positional argument (no '--' prefix) is the project name
            if (!args[1].StartsWith("--"))
                context.ProjectName = args[1];

            for (int i = 1; i < args.Length; i++)
            {
                var arg = args[i];

                if (!arg.StartsWith("--"))
                    continue;

                if (_knownFlags.Contains(arg))
                {
                    context.Flags.Add(arg);
                    continue;
                }

                // Key-value option: --key value
                if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                {
                    context.Options[arg] = args[i + 1];
                    i++;
                }
            }

            return context;
        }
    }
}