using System.Windows;
using System;
using System.Threading;

namespace NetworkProfileManager.Helpers;

internal static class StorageFeedback
{
    private static int _pending;
    internal static void Report()
    {
        Interlocked.Exchange(ref _pending, 1);
        // Preference callers may hold a store lease. Never wait synchronously for the UI here.
        try
        {
            var app = Application.Current;
            app?.Dispatcher.BeginInvoke(new Action(() => { if (app.MainWindow?.IsLoaded == true) Flush(); }));
        }
        catch (InvalidOperationException) { /* Closing dispatcher: preserve the pending warning, never fail a caller. */ }
    }
    internal static void Flush()
    {
        if (Interlocked.Exchange(ref _pending, 0) == 0) return;
        NotificationService.Warning(Loc.Get("Storage.SaveFailed", "Changes could not be saved. Existing data was preserved. Check the data folder and retry."));
    }
}
