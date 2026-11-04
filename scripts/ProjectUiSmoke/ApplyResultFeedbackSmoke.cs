using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;
using NetworkProfileManager.Views;

internal static partial class Program
{
    private static void CheckApplyResultFeedback(string output)
    {
        var inhibition = typeof(NetworkMutationLease).GetField("_inhibited", BindingFlags.NonPublic | BindingFlags.Static)!;
        foreach (var theme in new[] { "Dark", "Light" })
        foreach (var minimum in new[] { false, true })
        foreach (var kind in new[] { "expired", "blocked", "worker", "browser", "browser-journal" })
        {
            ApplyRenderTheme(theme);
            int original = (int)inhibition.GetValue(null)!;
            var f = new ApplyFixture { Fail = false, Consent = true, BrowserFails = kind.StartsWith("browser", StringComparison.Ordinal), FinishFails = kind == "browser-journal" };
            if (kind == "worker") f.EarlyResult = new(ApplyExecutionStatus.Unknown, true, "WorkerStillRunning");
            if (kind == "expired") f.EarlyResult = new(ApplyExecutionStatus.Failed, false, "Expired");
            var profile = new ProjectProfile { Name = "Fixture", Mode = "static", Ipv4 = new()
            { Address = "192.0.2.20", PrefixLength = 24, Dns = new() { Mode = "static", Servers = new() { "192.0.2.55" } } } };
            var vm = new ProjectApplyViewModel(profile, new ProjectDevice { Name = "Fixture panel", ProfileId = profile.Id, WebUrl = "https://192.0.2.44/" },
                f, f, f, _ => Task.FromResult<IReadOnlyList<ApplyAdapterIdentity>>(new[] { f.Snapshot.Identity }),
                () => Array.Empty<ProjectApplyRequest>(), f, verificationAttempts: 1);
            var window = new ProjectApplyWindow(vm) { Width = minimum ? 620 : 760, Height = minimum ? 520 : 760 };
            try
            {
                window.Show(); Pump(); vm.RefreshAsync().GetAwaiter().GetResult(); vm.SelectedAdapter = vm.Adapters.Single();
                vm.ReviewAsync(false).GetAwaiter().GetResult();
                if (kind == "blocked") NetworkMutationLease.Inhibit(); // This harness process only; restored below.
                vm.ApplyAsync().GetAwaiter().GetResult(); Pump();
                string expected = Loc.Get(kind switch
                {
                    "expired" => "Apply.Refused.Expired", "blocked" => "Apply.Refused.Inhibited",
                    "worker" => "Apply.WorkerActive", _ => "Apply.State.BrowserFailed"
                });
                if (!vm.Status.Contains(expected) || vm.Status.Contains(Loc.Get("Apply.ManualRecovery")))
                    throw new Exception($"Incorrect {kind} outcome guidance.");
                if ((kind == "browser-journal") != vm.Status.Contains(Loc.Get("Apply.JournalWarning")))
                    throw new Exception("Journal warning was hidden or invented.");
                if (kind.StartsWith("browser", StringComparison.Ordinal) ? f.Opened != 1 : f.Opened != 0)
                    throw new Exception("Unverified operation handed off the browser or verified failure lost its result.");
                var feedback = (TextBlock)window.FindName("ApplyFeedback");
                var bounds = feedback.TransformToAncestor(window).TransformBounds(new Rect(feedback.RenderSize));
                if (bounds.Top < 0 || bounds.Bottom > window.ActualHeight) throw new Exception("Outcome guidance is clipped.");
                Capture(window, Path.Combine(output, $"result-{kind}-{theme}-{(minimum ? "minimum" : "normal")}.png"));
            }
            finally { inhibition.SetValue(null, original); window.Close(); Pump(); }
        }
    }
}
