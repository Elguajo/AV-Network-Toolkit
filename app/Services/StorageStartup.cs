using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Views;

namespace NetworkProfileManager.Services;

internal static class StorageStartup
{
    internal static StorageRootContext? Run()
    {
        while (true)
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                bool elevated = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
                var locations = UserStorageLocations.ForUser(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppDomain.CurrentDomain.BaseDirectory);
                return new UserStorage(locations, otherInstance: OtherInstance,
                    beforeMigration: () => { if (elevated) throw new IOException("Storage.Elevated"); }).Open(Confirm);
            }
            catch (OperationCanceledException) { return null; }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException
                or ArgumentException or Newtonsoft.Json.JsonException or System.Security.SecurityException)
            {
                // Never present raw exception text, filenames from untrusted JSON, or data values.
                var reason = e.Message.StartsWith("Storage.", StringComparison.Ordinal) ? Loc.Get(e.Message, Loc.Get("Storage.Failed")) : Loc.Get("Storage.Failed");
                if (new StorageReviewWindow(Loc.Get("Storage.Unavailable"), reason + "\n\n" + Loc.Get("Storage.RetryHelp"),
                    Loc.Get("Common.Retry")).ShowDialog() != true) return null;
            }
        }
    }
    internal static bool Confirm(StorageReview review)
    {
        return new StorageReviewWindow(Loc.Get("Storage.Title"), ReviewMessage(review),
            Loc.Get(review.Diverged ? "Storage.KeepHome" : "Storage.Copy")).ShowDialog() == true;
    }
    internal static string ReviewMessage(StorageReview review)
    {
        return (review.Diverged ? Loc.Get("Storage.Diverged") : Loc.Get("Storage.CopyReview"))
            + "\n\n" + Loc.Get("Storage.Source") + "\n" + review.Source
            + "\n\n" + Loc.Get("Storage.Destination") + "\n" + review.Destination
            + "\n\n" + Loc.Get("Storage.Backup") + "\n" + review.Backup
            + "\n\n" + string.Format(Loc.Get("Storage.Count"), review.Files, review.Bytes)
            + "\n\n" + Loc.Get("Storage.Exclusions")
            + (review.UnavailableRecovery > 0 ? "\n\n" + string.Format(Loc.Get("Storage.RecoveryMissing"), review.UnavailableRecovery) : "");
    }
    private static bool OtherInstance()
    {
        using var current = Process.GetCurrentProcess();
        foreach (var process in Process.GetProcessesByName(current.ProcessName))
            using (process) { if (process.Id != current.Id) return true; }
        return false;
    }
}
