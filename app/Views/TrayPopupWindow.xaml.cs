using System;
using System.Windows;
using System.Windows.Shapes;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.ViewModels;
using Wpf.Ui.Controls;
using SWC = System.Windows.Controls;

namespace NetworkProfileManager.Views
{
    public partial class TrayPopupWindow : Window
    {
        public Action? OnBeforeClose { get; set; }
        private bool _isClosing;
        private readonly System.Drawing.Point _anchor;
        private readonly System.Drawing.Rectangle _workArea;

        public TrayPopupWindow() : this(System.Windows.Forms.Cursor.Position) { }

        internal TrayPopupWindow(System.Drawing.Point anchor)
        {
            _anchor = anchor;
            _workArea = System.Windows.Forms.Screen.FromPoint(anchor).WorkingArea;
            InitializeComponent();
            SourceInitialized += (s, e) =>
            {
                var transform = PresentationSource.FromVisual(this)!.CompositionTarget!.TransformFromDevice;
                var point = transform.Transform(new Point(_anchor.X, _anchor.Y));
                Left = point.X;
                Top = point.Y;
                MaxHeight = transform.Transform(new Vector(0, _workArea.Height)).Y * 0.80;
            };
            ContentRendered += (s, e) => PositionNearIcon();
            Deactivated += (s, e) => ClosePopup();
        }

        private void ClosePopup()
        {
            if (_isClosing) return;
            _isClosing = true;
            OnBeforeClose?.Invoke();
            Close();
        }

        // ── Build / rebuild content ──────────────────────────────────
        public void Rebuild(MainViewModel? vm, Action openMain, Action openSettings, Action exit)
        {
            ItemsPanel.Children.Clear();

            AddAction(Helpers.Loc.Get("Tray.OpenMain", "Pencereyi aç"), SymbolRegular.WindowNew20, openMain);

            if (vm != null)
            {
                bool anyProfiles = false;
                foreach (var adapter in vm.Adapters)
                {
                    if (adapter.Profiles.Count == 0) continue;
                    if (!anyProfiles)
                    {
                        AddSeparator();
                        anyProfiles = true;
                    }

                    AddAdapterLabel(adapter.Name);

                    foreach (var profile in adapter.Profiles)
                    {
                        var adp = adapter;
                        var pvm = profile;

                        AddProfileItem(pvm.Name, pvm.IpDisplay, pvm.IsActive, () =>
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                vm.SelectedAdapter = adp;
                                vm.ApplyProfileCommand.Execute(pvm);
                            });
                        });
                    }
                }
            }

            AddSeparator();
            AddAction(Helpers.Loc.Get("Tray.OpenSettings", "Ayarlar"), SymbolRegular.Settings20, openSettings);
            AddSeparator();
            AddAction(Helpers.Loc.Get("Tray.Exit", "Çıkış"), SymbolRegular.Dismiss20, exit);
        }

        // ── Item helpers ─────────────────────────────────────────────
        private void AddAction(string text, SymbolRegular symbol, Action onClick)
        {
            var icon = new SymbolIcon
            {
                Symbol = symbol,
                FontSize = 16,
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            icon.SetResourceReference(ForegroundProperty, "TextFillColorSecondaryBrush");

            var label = new SWC.TextBlock
            {
                Text = text,
                VerticalAlignment = VerticalAlignment.Center
            };

            var sp = new SWC.StackPanel { Orientation = SWC.Orientation.Horizontal };
            sp.Children.Add(icon);
            sp.Children.Add(label);

            AddButton(sp, onClick);
        }

        private void AddAdapterLabel(string name)
        {
            var tb = new SWC.TextBlock
            {
                Text = name,
                FontSize = 12,
                Margin = new Thickness(12, 8, 12, 2),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            tb.SetResourceReference(ForegroundProperty, "TextFillColorSecondaryBrush");
            ItemsPanel.Children.Add(tb);
        }

        private void AddProfileItem(string name, string ipDisplay, bool isActive, Action onClick)
        {
            // Like a checked menu item: the active profile gets a check, the
            // others keep the same indent so names line up.
            var check = new SymbolIcon
            {
                Symbol = SymbolRegular.Checkmark16,
                FontSize = 16,
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = isActive ? Visibility.Visible : Visibility.Hidden
            };

            var nameBlock = new SWC.TextBlock
            {
                Text = name,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            var ipBlock = new SWC.TextBlock
            {
                Text = ipDisplay,
                FontSize = 12,
                Visibility = string.IsNullOrEmpty(ipDisplay)
                                 ? Visibility.Collapsed
                                 : Visibility.Visible
            };
            ipBlock.SetResourceReference(ForegroundProperty, "TextFillColorSecondaryBrush");

            var textStack = new SWC.StackPanel { Orientation = SWC.Orientation.Vertical };
            textStack.Children.Add(nameBlock);
            textStack.Children.Add(ipBlock);

            var sp = new SWC.StackPanel { Orientation = SWC.Orientation.Horizontal };
            sp.Children.Add(check);
            sp.Children.Add(textStack);

            AddButton(sp, onClick);
        }

        private void AddButton(object content, Action onClick)
        {
            var btn = new SWC.Button
            {
                Content = content,
                Style = (Style)FindResource("TrayActionBtn")
            };
            btn.Click += (s, e) => { ClosePopup(); onClick(); };
            ItemsPanel.Children.Add(btn);
        }

        private void AddSeparator()
        {
            var rect = new Rectangle { Height = 1, Margin = new Thickness(-4, 4, -4, 4) };
            rect.SetResourceReference(Shape.FillProperty, "DividerStrokeColorDefaultBrush");
            ItemsPanel.Children.Add(rect);
        }

        // ── Positioning ──────────────────────────────────────────────
        private void PositionNearIcon()
        {
            // Cursor/Screen use screen pixels; WPF window coordinates use DIPs.
            var transform = PresentationSource.FromVisual(this)!.CompositionTarget!.TransformFromDevice;
            var anchor = transform.Transform(new Point(_anchor.X, _anchor.Y));
            var area = Rect.Transform(new Rect(_workArea.X, _workArea.Y, _workArea.Width, _workArea.Height), transform);
            MaxHeight = area.Height * 0.80;
            UpdateLayout();
            var position = TrayPopupPlacement.Calculate(anchor, area, new Size(ActualWidth, ActualHeight));
            Left = position.X;
            Top = position.Y;
        }
    }
}
