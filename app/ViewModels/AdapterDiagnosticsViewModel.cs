using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NetworkProfileManager.Commands;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;

namespace NetworkProfileManager.ViewModels;

public sealed class AdapterDiagnosticsViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IAdapterDiagnostics _reader;
    private readonly DiagnosticPingService _ping;
    private readonly Action<bool> _saveFilter;
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _pingCancellation;
    private IReadOnlyList<DiagnosticAdapter> _all = Array.Empty<DiagnosticAdapter>();
    private DiagnosticAdapter? _selected;
    private string? _source;
    private bool _busy, _disposed, _hide;
    private string _target = "", _status = "";
    public AdapterDiagnosticsViewModel(IAdapterDiagnostics reader, DiagnosticPingService ping,
        bool hideUplinks = false, Action<bool>? saveFilter = null)
    {
        _reader = reader; _ping = ping; _hide = hideUplinks; _saveFilter = saveFilter ?? (_ => { });
        RefreshCommand = new RelayCommand(async _ => await RefreshAsync(), _ => CanSelect);
        PingCommand = new RelayCommand(async _ => await PingAsync(ProbeCount), _ => CanPing);
        StopCommand = new RelayCommand(_ => _pingCancellation?.Cancel(), _ => _pingCancellation != null);
        Status = Loc.Get("Diagnostics.RefreshHint");
    }
    internal static AdapterDiagnosticsViewModel Create(bool persist = true) => new(new WindowsAdapterDiagnostics(),
        new(new WindowsDiagnosticPingPlatform()), persist && AppSettingsStore.Get("HideHyperVUplinks") == "true",
        persist ? value => AppSettingsStore.Set("HideHyperVUplinks", value ? "true" : "false") : null);
    public IReadOnlyList<DiagnosticAdapter> Adapters => _all.Where(a => !_hide || a.HyperVUplink != true).ToArray();
    public IReadOnlyList<string> Sources => _selected?.Addresses.Where(a => a.State == "Preferred" && !a.SkipAsSource
        && DiagnosticPingService.IsUnicast(a.Address, out _)).Select(a => a.Address).Distinct().ToArray() ?? Array.Empty<string>();
    public ObservableCollection<DiagnosticPingResult> Results { get; } = new();
    private Guid? PreferredAdapterId { get; set; }
    public int ProbeCount { get; set; } = 1;
    internal AdapterDiagnosticsViewModel CreatePeer() => new(_reader, _ping, _hide, _saveFilter)
        { PreferredAdapterId = _selected?.Id ?? PreferredAdapterId, Target = Target, ProbeCount = 4 };
    public DiagnosticAdapter? SelectedAdapter
    {
        get => _selected;
        set
        {
            if (!CanSelect || ReferenceEquals(value, _selected)) return;
            _selected = value != null && Adapters.Contains(value) ? value : null;
            _source = null; Changed(); Changed(nameof(Sources)); Changed(nameof(Source)); Changed(nameof(Details)); Changed(nameof(CanPing));
        }
    }
    public string? Source { get => _source; set { if (!CanSelect) return; _source = value != null && Sources.Contains(value) ? value : null; Changed(); Changed(nameof(CanPing)); } }
    public string Target { get => _target; set { if (!CanSelect) return; _target = value; Changed(); Changed(nameof(CanPing)); } }
    public bool HideHyperVUplinks
    {
        get => _hide;
        set
        {
            if (!CanSelect || value == _hide) return;
            _hide = value; Changed(); Changed(nameof(Adapters));
            if (_selected?.HyperVUplink == true && value) SelectedAdapter = null;
            try { _saveFilter(value); } catch { Status = Loc.Get("Diagnostics.FilterUnsaved"); }
        }
    }
    public bool CanSelect => !_disposed && !_busy;
    public bool CanPing => CanSelect && _selected != null && _source != null && DiagnosticPingService.ValidTarget(_target.Trim());
    public string Status { get => _status; private set { _status = value; Changed(); } }
    public string Details => _selected == null ? Loc.Get("Diagnostics.Select")
        : Loc.Format("Diagnostics.Facts", _selected.Id, _selected.Index, _selected.Mac, _selected.KindDisplay,
            _selected.Link, _selected.Speed, _selected.Dhcp,
            _selected.HyperVUplink.HasValue ? Loc.Get(_selected.HyperVUplink.Value ? "Diagnostics.Yes" : "Diagnostics.No") : Loc.Get("Diagnostics.Unknown"))
          + "\n" + Loc.Format("Diagnostics.Addresses", string.Join("; ", _selected.Addresses.Select(a => a.Display)))
          + "\n" + Loc.Format("Diagnostics.Dns", string.Join(", ", _selected.Dns))
          + "\n" + Loc.Get("Diagnostics.Routes") + "\n" + string.Join("\n", _selected.Routes.Select(r => $"{r.Destination} → {r.NextHop} · {r.Metric}"));
    public ICommand RefreshCommand { get; }
    public ICommand PingCommand { get; }
    public ICommand StopCommand { get; }
    public async Task RefreshAsync()
    {
        if (!CanSelect) return;
        var selectedId = _selected?.Id ?? PreferredAdapterId;
        _selected = null; _source = null; _all = Array.Empty<DiagnosticAdapter>();
        Changed(nameof(Adapters)); Changed(nameof(SelectedAdapter)); Changed(nameof(Sources)); Changed(nameof(Source)); Changed(nameof(Details));
        SetBusy(true); Status = Loc.Get("Diagnostics.Loading");
        try
        {
            var adapters = await _reader.ReadAsync(_lifetime.Token);
            if (_disposed) return;
            _all = adapters; Changed(nameof(Adapters));
            Status = Loc.Get(adapters.Count == 0 ? "Diagnostics.Empty" : "Diagnostics.Select");
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch { if (!_disposed) Status = Loc.Get("Diagnostics.Error.Unavailable"); }
        finally
        {
            SetBusy(false);
            if (!_disposed && selectedId.HasValue) SelectedAdapter = Adapters.SingleOrDefault(a => a.Id == selectedId);
        }
    }
    public async Task PingAsync(int count = 1)
    {
        if (!CanPing || count is < 1 or > 4) return;
        var adapter = _selected!;
        var request = new DiagnosticPingRequest(adapter.Id, adapter.Index, adapter.Mac, adapter.Name, _source!, _target.Trim());
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _pingCancellation = cancellation;
        SetBusy(true); Results.Clear(); Status = Loc.Get("Diagnostics.Pinging");
        try
        {
            for (int i = 0; i < count; i++)
            {
                var result = await Task.Run(() => _ping.SendAsync(request, cancellation.Token), cancellation.Token);
                if (_disposed) return;
                Results.Add(result);
                if (result.Error.Length != 0 || cancellation.IsCancellationRequested) break;
                if (i + 1 < count) await Task.Delay(1000, cancellation.Token);
            }
            if (!_disposed) Status = Results.All(r => !r.Success) ? Loc.Get("Diagnostics.NoReplies")
                : Loc.Format("Diagnostics.Summary", Results.Count(r => r.Success), Results.Count,
                Results.Where(r => r.Success).Select(r => r.Echo!.Milliseconds).DefaultIfEmpty(0).Min(),
                Results.Where(r => r.Success).Select(r => r.Echo!.Milliseconds).DefaultIfEmpty(0).Max(),
                Results.Where(r => r.Success).Select(r => r.Echo!.Milliseconds).DefaultIfEmpty(0).Average());
        }
        catch (OperationCanceledException) { if (!_disposed) Status = Loc.Get("Diagnostics.Error.Cancelled"); }
        finally { _pingCancellation = null; SetBusy(false); }
    }
    private void SetBusy(bool value)
    {
        _busy = value;
        if (_disposed) return;
        Changed(nameof(CanSelect)); Changed(nameof(CanPing)); CommandManager.InvalidateRequerySuggested();
    }
    public void Dispose() { if (_disposed) return; _disposed = true; _lifetime.Cancel(); _lifetime.Dispose(); }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new(name));
        if (name == nameof(CanPing)) CommandManager.InvalidateRequerySuggested();
    }
}
