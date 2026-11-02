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
            UserStorageLocations? locations = null;
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                bool elevated = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
                locations = UserStorageLocations.ForUser(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppDomain.CurrentDomain.BaseDirectory);
                return new UserStorage(locations, otherInstance: OtherInstance,
                    beforeMigration: () => { if (elevated) throw new IOException("Storage.Elevated"); }).Open(Confirm);
            }
            catch (OperationCanceledException) { return null; }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException
                or ArgumentException or Newtonsoft.Json.JsonException or System.Security.SecurityException)
            {
                if (new StorageReviewWindow(Loc.Get("Storage.Unavailable"), FailureMessage(e, locations),
                    Loc.Get("Common.Retry")).ShowDialog() != true) return null;
            }
        }
    }
    internal static string FailureMessage(Exception error, UserStorageLocations? locations)
    {
        // Known roots help recovery; never display raw exception text, untrusted filenames or data values.
        var reason = error.Message.StartsWith("Storage.", StringComparison.Ordinal)
            ? Loc.Get(error.Message, Loc.Get("Storage.Failed")) : Loc.Get("Storage.Failed");
        return reason + (locations == null ? "" : "\n\n" + Loc.Get("Storage.Destination") + "\n" + locations.Home
            + "\n\n" + Loc.Get("Storage.Source") + "\n" + locations.Legacy
            + "\n\n" + Loc.Get("Storage.Backup") + "\n" + locations.Backups)
            + "\n\n" + Loc.Get("Storage.RetryHelp");
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
