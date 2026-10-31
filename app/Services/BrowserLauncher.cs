using System;
using System.Diagnostics;

namespace NetworkProfileManager.Services;

public interface IBrowserLauncher
{
    void Open(string destination);
}

public sealed class DefaultBrowserLauncher : IBrowserLauncher
{
    private readonly Func<ProcessStartInfo, Process?> _start;
    public DefaultBrowserLauncher() : this(Process.Start) { }
    internal DefaultBrowserLauncher(Func<ProcessStartInfo, Process?> start) => _start = start;
    public void Open(string destination)
    {
        // Recheck the same policy at the OS boundary. Pass the original string as
        // a document, never as arguments to cmd, PowerShell or a named browser.
        ProjectJson.ValidateUrl(destination);
        // ShellExecute may return no process handle when a running browser
        // accepts the document. Only thrown errors establish launch failure.
        using var process = _start(new ProcessStartInfo { FileName = destination, UseShellExecute = true });
    }
}
