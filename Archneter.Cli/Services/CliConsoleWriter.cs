namespace Archneter.Cli.Services
{
    public sealed class CliConsoleWriter
    {
        public void WriteHeader(string title)
        {
            Console.WriteLine();
            Console.WriteLine($"{title.ToUpperInvariant()}:");
        }

        public void WriteLine(string text = "")
        {
            Console.WriteLine(text);
        }

        public void WriteRow(string col1, string col2, int indent = 2, int col1Width = 30)
        {
            var indentStr = new string(' ', indent);
            var padded = col1.PadRight(col1Width);
            Console.WriteLine($"{indentStr}{padded}{col2}");
        }
    }
}
