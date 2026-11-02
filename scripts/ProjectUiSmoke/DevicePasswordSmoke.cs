using System.IO;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;
using NetworkProfileManager.Views;

internal static partial class Program
{
    private static void CheckDevicePasswords(string output)
    {
        foreach (var theme in new[] { "Dark", "Light" })
        foreach (var minimum in new[] { false, true })
        {
            ApplyRenderTheme(theme);
            var vm = new ProjectViewModel(new FakeDialogs(), new ProjectStore(), new FakeBrowser(), _ => true);
            vm.DeviceEditor.Name = "Example control panel";
            vm.DeviceEditor.WebUrl = "https://192.0.2.20/panel";
            vm.DeviceEditor.SavePassword = true;
            vm.DeviceEditor.Password = "SYNTHETIC-UI-ONLY";
            vm.KeepDeviceCommand.Execute(null); vm.Section = 1;
            var window = new ProjectWindow(vm) { Width = minimum ? 700 : 1000, Height = minimum ? 540 : 850 };
            try
            {
                window.Show(); Pump();
                var view = Descendants(window).OfType<DeviceDirectoryView>().Single(v => v.IsVisible);
                var input = (PasswordBox)view.FindName("DevicePasswordInput");
                var revealed = (TextBlock)view.FindName("DeviceRevealedPassword");
                input.BringIntoView(); Pump();
                if (input.Password != "SYNTHETIC-UI-ONLY" || revealed.Text.Length != 0 || revealed.IsVisible)
                    throw new Exception("Loaded credential was not masked.");
                var peer = UIElementAutomationPeer.CreatePeerForElement(input);
                if (!peer.IsPassword()) throw new Exception("Masked input did not expose password semantics.");
                var provider = peer.GetPattern(PatternInterface.Value) as IValueProvider;
                bool denied = provider == null;
                try { if (provider != null) denied = provider.Value.Length == 0; }
                catch (InvalidOperationException) { denied = true; }
                if (!denied) throw new Exception("Masked input exposed an automation value.");
                Capture(window, Path.Combine(output, $"password-stored-{theme}-{window.Width}.png"));
                var copyButton = Descendants(view).OfType<System.Windows.Controls.Button>().Single(b => b.Command == vm.CopyPasswordCommand);
                copyButton.BringIntoView(); Pump();
                var viewport = FindParentScroll(copyButton);
                var bounds = copyButton.TransformToAncestor(viewport).TransformBounds(new Rect(copyButton.RenderSize));
                if (bounds.Top < 0 || bounds.Bottom > viewport.ActualHeight + 1)
                    throw new Exception("Password actions are unreachable in the editor viewport.");
                Capture(window, Path.Combine(output, $"password-actions-{theme}-{window.Width}.png"));
                bool dirty = vm.HasUnsavedChanges;
                vm.RevealPasswordCommand.Execute(null); Pump();
                if (!revealed.IsVisible || revealed.Text != "SYNTHETIC-UI-ONLY")
                    throw new Exception("Explicit reveal did not expose the current draft.");
                // Never capture revealed values, including synthetic fixtures.
                vm.Section = 0; Pump(); vm.Section = 1; Pump();
                if (revealed.Text.Length != 0 || vm.DeviceEditor.IsPasswordRevealed)
                    throw new Exception("Leaving device section did not clear revealed text.");
                vm.CopyPasswordCommand.Execute(null); Pump();
                if (vm.HasUnsavedChanges != dirty) throw new Exception("Show/copy mutated the document.");
                vm.DeviceEditor.Password = ""; vm.KeepDeviceCommand.Execute(null); Pump();
                if (vm.Error.Length == 0 || vm.Devices[0].Password != "SYNTHETIC-UI-ONLY")
                    throw new Exception("Clearing input silently removed a credential.");
                input.BringIntoView(); Pump();
                Capture(window, Path.Combine(output, $"password-invalid-{theme}-{window.Width}.png"));
                input.Password = new string('x', 2049); Pump(); vm.KeepDeviceCommand.Execute(null); Pump();
                if (vm.DeviceEditor.Password.Length != 2049 || vm.Error.Length == 0)
                    throw new Exception("Pasted input was silently truncated instead of rejected.");
                vm.RemovePasswordCommand.Execute(null); vm.KeepDeviceCommand.Execute(null); Pump();
                if (vm.Devices[0].Password != null || input.Password.Length != 0)
                    throw new Exception("Explicit removal did not update the masked editor.");
                input.BringIntoView(); Pump();
                Capture(window, Path.Combine(output, $"password-removed-{theme}-{window.Width}.png"));
                vm.NewDeviceCommand.Execute(null); Pump();
                if (vm.DeviceEditor.SavePassword || input.Password.Length != 0 || vm.CopyPasswordCommand.CanExecute(null))
                    throw new Exception("New device inherited credential/command state.");
                input.BringIntoView(); Pump();
                Capture(window, Path.Combine(output, $"password-new-{theme}-{window.Width}.png"));
            }
            finally { window.Close(); Pump(); }
        }
    }
    private static ScrollViewer FindParentScroll(DependencyObject child)
    {
        for (var parent = System.Windows.Media.VisualTreeHelper.GetParent(child); parent != null;
             parent = System.Windows.Media.VisualTreeHelper.GetParent(parent))
            if (parent is ScrollViewer scroll) return scroll;
        throw new Exception("Missing editor scroll viewport.");
    }
}
