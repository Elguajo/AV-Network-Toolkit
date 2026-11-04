using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.ViewModels;
using NetworkProfileManager.Views;

internal static partial class Program
{
    private static void CheckProjectDiscard(string output)
    {
        foreach (var theme in new[] { "Dark", "Light" })
        foreach (var minimum in new[] { false, true })
        {
            ApplyRenderTheme(theme);
            var root = new ProjectWindow { Width = 700, Height = 540 };
            var vm = (ProjectViewModel)root.DataContext;
            try
            {
                root.Show(); Pump();
                if (!vm.CanDiscard()) throw new Exception("Clean project unexpectedly needs confirmation.");
                foreach (var action in new[] { "cancel", "enter", "escape", "close", "discard-enter", "discard" })
                {
                    vm.ProjectName = "Unsaved fixture";
                    WithDiscardPrompt(root, () => vm.NewCommand.Execute(null), dialog =>
                    {
                        if (dialog.Owner != root) throw new Exception("Project discard lost its owner.");
                        if (minimum) { dialog.Width = dialog.MinWidth; dialog.Height = dialog.MinHeight; Pump(); }
                        AssertDiscardSurface(dialog, theme);
                        if (action == "cancel") Capture(dialog, System.IO.Path.Combine(output, $"discard-{theme}-{(minimum ? "minimum" : "normal")}.png"));
                        switch (action)
                        {
                            case "cancel": ClickDiscardAction(dialog, "CancelButton"); break;
                            case "discard": ClickDiscardAction(dialog, "DiscardButton"); break;
                            case "discard-enter":
                                ((Wpf.Ui.Controls.Button)dialog.FindName("DiscardButton")).Focus();
                                DiscardKey(dialog, Key.Enter); break;
                            case "enter": DiscardKey(dialog, Key.Enter); break;
                            case "escape": DiscardKey(dialog, Key.Escape); break;
                            default: dialog.Close(); break;
                        }
                        if (dialog.IsVisible) throw new Exception($"{action} did not close the confirmation.");
                    });
                    if (action.StartsWith("discard", StringComparison.Ordinal))
                    {
                        // A newly created, not-yet-saved project is intentionally dirty.
                        if (vm.ProjectName != Loc.Get("Project.NewName") || vm.Profiles.Count != 0 || vm.Devices.Count != 0)
                            throw new Exception("Explicit discard did not reach New project.");
                    }
                    else if (!vm.HasUnsavedChanges || vm.ProjectName != "Unsaved fixture")
                        throw new Exception($"{action} discarded project changes.");
                }

                // A shared VM retains the root dialog service; the actual modal
                // editor must still own the confirmation, including at depth two.
                var parent = new Wpf.Ui.Controls.FluentWindow { Owner = root, Width = 700, Height = 540, ShowInTaskbar = false };
                parent.Show(); Pump();
                var device = new ProjectDeviceDialog(vm) { Owner = parent };
                CheckDiscardEditor(root, device, () => device.Draft.Name = "Detached device", () => device.ConfirmApplicationExit(),
                    () => device.Draft.Name == "Detached device" && vm.Devices.Count == 0);
                parent.Close();
                var profile = new ProjectProfileDialog(vm) { Owner = root };
                CheckDiscardEditor(root, profile, () => ((ProjectProfileEditor)profile.DataContext).Name = "Detached profile",
                    () => profile.ConfirmApplicationExit(), () => ((ProjectProfileEditor)profile.DataContext).Name == "Detached profile" && vm.Profiles.Count == 0);
            }
            finally { root.ApproveApplicationClose(); root.Close(); Pump(); }
        }
    }

    private static void CheckDiscardEditor(Window root, Window editor, Action dirty, Func<bool> canExit, Func<bool> preserved)
    {
        editor.Show(); editor.Activate(); Pump(); dirty();
        var field = Descendants(editor).OfType<TextBox>().First(t => t.IsVisible);
        field.Focus(); Pump();
        bool approved = true;
        WithDiscardPrompt(root, () => approved = canExit(), dialog =>
        {
            if (dialog.Owner != editor) throw new Exception("Confirmation must belong to the active nested editor.");
            var original = Wpf.Ui.Appearance.ApplicationThemeManager.GetAppTheme().ToString();
            ApplyRenderTheme(original == "Dark" ? "Light" : "Dark"); Pump();
            AssertWindowTheme(dialog, original == "Dark" ? "Light" : "Dark");
            ApplyRenderTheme(original); Pump();
            ClickDiscardAction(dialog, "CancelButton");
        });
        if (approved || !editor.IsVisible || !preserved() || !field.IsKeyboardFocusWithin)
            throw new Exception("Cancel lost the detached draft, exit guard or editor focus.");
        WithDiscardPrompt(root, editor.Close, dialog => DiscardKey(dialog, Key.Escape));
        if (!editor.IsVisible || !preserved()) throw new Exception("Escape allowed a dirty editor to close.");
        WithDiscardPrompt(root, editor.Close, dialog => ClickDiscardAction(dialog, "DiscardButton"));
        if (editor.IsVisible) throw new Exception("Explicit editor discard was rejected.");
        Pump();
        if (!preserved()) throw new Exception("Discarding a detached editor changed retained data.");
    }

    private static void AssertDiscardSurface(ProjectDiscardDialog dialog, string theme)
    {
        AssertWindowTheme(dialog, theme);
        var cancel = (Wpf.Ui.Controls.Button)dialog.FindName("CancelButton");
        var discard = (Wpf.Ui.Controls.Button)dialog.FindName("DiscardButton");
        if (!cancel.IsDefault || !cancel.IsCancel || discard.IsDefault || Keyboard.FocusedElement != cancel)
            throw new Exception("Discard must require a deliberate choice; focus/default/cancel must stay on Cancel.");
        if (AutomationProperties.GetName(cancel) != Loc.Get("Common.Cancel")
            || AutomationProperties.GetName(discard) != Loc.Get("Project.DiscardAction"))
            throw new Exception("Discard action accessible names are missing.");
        foreach (var button in new[] { cancel, discard })
        {
            var rect = button.TransformToAncestor(dialog).TransformBounds(new Rect(button.RenderSize));
            if (rect.Left < 0 || rect.Top < 0 || rect.Right > dialog.ActualWidth || rect.Bottom > dialog.ActualHeight)
                throw new Exception("Discard action is clipped.");
        }
        cancel.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
        if (Keyboard.FocusedElement != discard) throw new Exception("Discard is not keyboard reachable.");
        cancel.Focus();
    }

    private static void WithDiscardPrompt(Window root, Action trigger, Action<ProjectDiscardDialog> inspect)
    {
        Exception? failure = null;
        bool seen = false;
        root.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
        {
            var dialog = Application.Current.Windows.OfType<ProjectDiscardDialog>().SingleOrDefault(w => w.IsVisible);
            try
            {
                if (dialog == null) throw new Exception("The Fluent discard dialog did not open.");
                seen = true; Pump(); inspect(dialog);
            }
            catch (Exception e) { failure = e; }
            finally { if (dialog?.IsVisible == true) dialog.Close(); }
        }));
        trigger(); Pump();
        if (failure != null) throw new Exception("Discard confirmation failed.", failure);
        if (!seen) throw new Exception("Expected discard confirmation was bypassed.");
    }

    private static void ClickDiscardAction(ProjectDiscardDialog dialog, string name) =>
        ((Wpf.Ui.Controls.Button)dialog.FindName(name)).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

    private static void DiscardKey(Window window, Key key)
    {
        var source = PresentationSource.FromVisual(window);
        InputManager.Current.ProcessInput(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, key) { RoutedEvent = Keyboard.KeyDownEvent });
        InputManager.Current.ProcessInput(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, key) { RoutedEvent = Keyboard.KeyUpEvent });
        Pump();
    }
}
