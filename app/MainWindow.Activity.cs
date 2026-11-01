using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using NetworkProfileManager.ViewModels;
using NetworkProfileManager.Views;

namespace NetworkProfileManager;

public partial class MainWindow
{
    private readonly HashSet<AdapterDiagnosticsViewModel> _diagnosticViews = new();
    private readonly HashSet<AdapterDiagnosticsViewModel> _retiredDiagnosticViews = new();
    private MdnsDiscoveryViewModel? _listeningView;
    public static readonly DependencyProperty AreTestsRunningProperty = DependencyProperty.Register(
        nameof(AreTestsRunning), typeof(bool), typeof(MainWindow), new PropertyMetadata(false));
    public static readonly DependencyProperty IsMdnsListeningProperty = DependencyProperty.Register(
        nameof(IsMdnsListening), typeof(bool), typeof(MainWindow), new PropertyMetadata(false));
    public bool AreTestsRunning { get => (bool)GetValue(AreTestsRunningProperty); private set => SetValue(AreTestsRunningProperty, value); }
    public bool IsMdnsListening { get => (bool)GetValue(IsMdnsListeningProperty); private set => SetValue(IsMdnsListeningProperty, value); }

    private void InitializeToolActivity(AdapterDiagnosticsViewModel inline)
    {
        ObserveDiagnostics(inline);
        Closed += (_, _) =>
        {
            foreach (var vm in _diagnosticViews) vm.PropertyChanged -= DiagnosticActivityChanged;
            _diagnosticViews.Clear(); _retiredDiagnosticViews.Clear();
            if (_listeningView != null) _listeningView.PropertyChanged -= MdnsActivityChanged;
        };
    }
    private void ObserveDiagnostics(AdapterDiagnosticsViewModel vm)
    {
        if (_diagnosticViews.Add(vm)) vm.PropertyChanged += DiagnosticActivityChanged;
        UpdateDiagnosticActivity();
    }
    internal void TrackDiagnosticWindow(PingWindow window, AdapterDiagnosticsViewModel vm)
    {
        ObserveDiagnostics(vm);
        window.Closed += (_, _) =>
        {
            _retiredDiagnosticViews.Add(vm);
            UpdateDiagnosticActivity();
        };
    }
    private void DiagnosticActivityChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AdapterDiagnosticsViewModel.IsTestRunning)) UpdateDiagnosticActivity();
    }
    private void UpdateDiagnosticActivity()
    {
        // Keep a closed peer until an already-entered native call has actually drained.
        foreach (var vm in _retiredDiagnosticViews.Where(v => !v.IsTestRunning).ToArray())
        {
            vm.PropertyChanged -= DiagnosticActivityChanged;
            _diagnosticViews.Remove(vm); _retiredDiagnosticViews.Remove(vm);
        }
        AreTestsRunning = _diagnosticViews.Any(v => v.IsTestRunning);
    }
    internal void TrackMdnsWindow(MdnsDiscoveryWindow window)
    {
        var vm = (MdnsDiscoveryViewModel)window.DataContext;
        _listeningView = vm;
        vm.PropertyChanged += MdnsActivityChanged;
        IsMdnsListening = vm.IsListening;
        window.Closed += (_, _) =>
        {
            vm.PropertyChanged -= MdnsActivityChanged;
            if (ReferenceEquals(_listeningView, vm)) { _listeningView = null; IsMdnsListening = false; }
        };
    }
    private void MdnsActivityChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MdnsDiscoveryViewModel.IsListening)) IsMdnsListening = _listeningView?.IsListening == true;
    }
}
