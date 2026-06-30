using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Archneter.Cli.Attributes;
using Archneter.Cli.Models;
using Archneter.Cli.Parsing;
using Archneter.Cli.Services;

namespace Archneter.Cli.Commands;

[Command("ui")]
[Description("Launch the Archneter Web Dashboard")]
[CommandSyntax("ui")]
[CommandExample("archneter ui")]
public sealed class UiCommand : IArchCommand
{
    private readonly CommandDispatcher _dispatcher;
    private readonly ArgumentParser _parser;

    public UiCommand(CommandDispatcher dispatcher, ArgumentParser parser)
    {
        _dispatcher = dispatcher;
        _parser = parser;
    }

    public async Task ExecuteAsync(CommandContext context)
    {
        string url = "http://localhost:8999/";
        using var listener = new HttpListener();
        listener.Prefixes.Add(url);

        try
        {
            listener.Start();
            Console.WriteLine($"[INFO] Archneter UI started at {url}");
            Console.WriteLine("Press Ctrl+C to stop.");

            OpenBrowser(url);

            while (true)
            {
                var ctx = await listener.GetContextAsync();
                _ = Task.Run(() => HandleRequest(ctx));
            }
        }
        catch (HttpListenerException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Failed to start UI: {ex.Message}");
            Console.ResetColor();
        }
    }

    private async Task HandleRequest(HttpListenerContext context)
    {
        var req = context.Request;
        var res = context.Response;

        res.AppendHeader("Access-Control-Allow-Origin", "*");

        try
        {
            if (req.HttpMethod == "GET" && req.Url?.AbsolutePath == "/")
            {
                byte[] buffer = Encoding.UTF8.GetBytes(UiTemplate.Html);
                res.ContentType = "text/html";
                res.ContentLength64 = buffer.Length;
                await res.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            else if (req.HttpMethod == "POST" && req.Url?.AbsolutePath == "/api/execute")
            {
                using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                var body = await reader.ReadToEndAsync();
                var payload = JsonSerializer.Deserialize<UiPayload>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var targetDir = string.IsNullOrWhiteSpace(payload?.TargetDirectory) 
                    ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop) 
                    : payload.TargetDirectory;

                string output = await ExecuteCommandIntercepted(payload?.CommandArgs ?? "", targetDir);

                var responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { output, actualTarget = targetDir }));
                res.ContentType = "application/json";
                res.ContentLength64 = responseBytes.Length;
                await res.OutputStream.WriteAsync(responseBytes, 0, responseBytes.Length);
            }
            else if (req.HttpMethod == "POST" && req.Url?.AbsolutePath == "/api/open")
            {
                using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                var body = await reader.ReadToEndAsync();
                var payload = JsonSerializer.Deserialize<UiPayload>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (!string.IsNullOrWhiteSpace(payload?.TargetDirectory))
                {
                    OpenBrowser(payload.TargetDirectory); // Opens the folder in OS explorer
                }
                res.StatusCode = 200;
            }
            else
            {
                res.StatusCode = 404;
            }
        }
        catch (Exception ex)
        {
            res.StatusCode = 500;
            var err = Encoding.UTF8.GetBytes(ex.Message);
            await res.OutputStream.WriteAsync(err, 0, err.Length);
        }
        finally
        {
            res.Close();
        }
    }

    private async Task<string> ExecuteCommandIntercepted(string args, string? targetDirectory)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var sw = new StringWriter();
        Console.SetOut(sw);
        Console.SetError(sw);

        string originalDir = Directory.GetCurrentDirectory();

        try
        {
            if (!string.IsNullOrWhiteSpace(targetDirectory))
            {
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }
                Directory.SetCurrentDirectory(targetDirectory);
            }

            var parts = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 && parts[0].ToLower() == "archneter")
                parts = parts.Skip(1).ToArray();

            var cliContext = _parser.Parse(parts);

            if (string.IsNullOrEmpty(cliContext.Command))
            {
                Console.WriteLine("No command specified.");
            }
            else
            {
                // Dispatch command
                await _dispatcher.DispatchAsync(cliContext.Command, cliContext);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[Error] {ex.Message}");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }

        return sw.ToString();
    }

    private void OpenBrowser(string url)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
        }
        catch { /* Ignore */ }
    }
}

public class UiPayload
{
    public string CommandArgs { get; set; }
    public string TargetDirectory { get; set; }
}
