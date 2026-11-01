using System.Security.Principal;

namespace NetworkProfileManager.Helpers;

public static class ElevationHelper
{
    public const string WorkerSwitch = "--run-netsh";
    public static bool IsElevated
    {
        get
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
    // Compatibility rejection only. Never read a request, write a result, or execute commands.
    public static int RunWorker(string requestPath, string resultPath) => 1;
}
