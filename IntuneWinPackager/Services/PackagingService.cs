using System.Diagnostics;

namespace IntuneWinPackager.Services;

public static class PackagingService
{
    /// <summary>
    /// Runs IntuneWinAppUtil.exe asynchronously with real-time output streaming.
    /// </summary>
    /// <param name="exePath">Path to IntuneWinAppUtil.exe</param>
    /// <param name="sourceFolder">Source folder (-c)</param>
    /// <param name="setupFile">Setup file (-s)</param>
    /// <param name="outputFolder">Output folder (-o)</param>
    /// <param name="catalogFolder">Optional catalog folder (-a)</param>
    /// <param name="isQuiet">Quiet mode (-q)</param>
    /// <param name="isSuperQuiet">Super quiet mode (-qq)</param>
    /// <param name="onOutput">Callback for each line of stdout/stderr</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The full command string that was executed, and the exit code</returns>
    public static async Task<(string Command, int ExitCode)> RunAsync(
        string exePath,
        string sourceFolder,
        string setupFile,
        string outputFolder,
        string? catalogFolder,
        bool isQuiet,
        bool isSuperQuiet,
        Action<string> onOutput,
        CancellationToken cancellationToken = default)
    {
        // Build argument string — quote all paths to handle spaces
        var args = $"-c \"{sourceFolder}\" -s \"{setupFile}\" -o \"{outputFolder}\"";

        if (!string.IsNullOrWhiteSpace(catalogFolder))
            args += $" -a \"{catalogFolder}\"";

        if (isSuperQuiet)
            args += " -qq";
        else if (isQuiet)
            args += " -q";

        var command = $"\"{exePath}\" {args}";

        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = new Process { StartInfo = psi };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                onOutput(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                onOutput($"[ERROR] {e.Data}");
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        return (command, process.ExitCode);
    }
}
