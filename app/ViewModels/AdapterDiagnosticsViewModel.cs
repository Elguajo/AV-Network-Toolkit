using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
    private readonly DiagnosticTcpService? _tcp;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;
    private readonly Action<bool> _saveFilter;
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _pingCancellation;
    private IReadOnlyList<DiagnosticAdapter> _all = Array.Empty<DiagnosticAdapter>();
    private DiagnosticAdapter? _selected;
    private string? _source;
    private bool _busy, _disposed, _hide;
    private string _target = "", _status = "";
    private string _port = "443";
    private DiagnosticTestPreset _test = new(DiagnosticTestKind.PingFour);
    private DiagnosticTcpResult? _tcpResult;
    private long _attempts, _replies, _minRtt, _maxRtt;
    private double _totalRtt;
    public AdapterDiagnosticsViewModel(IAdapterDiagnostics reader, DiagnosticPingService ping,
        bool hideUplinks = false, Action<bool>? saveFilter = null, DiagnosticTcpService? tcp = null,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        _reader = reader; _ping = ping; _hide = hideUplinks; _saveFilter = saveFilter ?? (_ => { });
        _tcp = tcp; _delay = delay ?? Task.Delay;
        RefreshCommand = new RelayCommand(async _ => await RefreshAsync(), _ => CanSelect);
        PingCommand = new RelayCommand(async _ => await PingAsync(ProbeCount), _ => CanPing);
        RunTestCommand = new RelayCommand(async _ => await RunSelectedTestAsync(), _ => CanRun);
        QuickPortCommand = new RelayCommand(value => Port = value as string ?? "", _ => CanSelect);
        StopCommand = new RelayCommand(_ => { _pingCancellation?.Cancel(); Changed(nameof(CanStop)); }, _ => CanStop);
        Status = Loc.Get("Diagnostics.RefreshHint");
    }
    internal static AdapterDiagnosticsViewModel Create(bool persist = true)
    {
        var platform = new WindowsDiagnosticPingPlatform();
        return new(new WindowsAdapterDiagnostics(), new(platform), persist && AppSettingsStore.Get("HideHyperVUplinks") == "true",
            persist ? value => AppSettingsStore.Set("HideHyperVUplinks", value ? "true" : "false") : null,
            new(platform, new WindowsDiagnosticTcpConnector()));
    }
    public IReadOnlyList<DiagnosticAdapter> Adapters => _all.Where(a => !_hide || a.HyperVUplink != true).ToArray();
    public IReadOnlyList<string> Sources => _selected?.Addresses.Where(a => a.State == "Preferred" && !a.SkipAsSource
        && DiagnosticPingService.IsUnicast(a.Address, out _)).Select(a => a.Address).Distinct().ToArray() ?? Array.Empty<string>();
    public ObservableCollection<DiagnosticPingResult> Results { get; } = new();
    public IReadOnlyList<DiagnosticTestPreset> Presets { get; } = new[]
    {
        new DiagnosticTestPreset(DiagnosticTestKind.PingFour), new(DiagnosticTestKind.ContinuousPing),
        new(DiagnosticTestKind.TcpPort), new(DiagnosticTestKind.PingOnce)
    };
    public DiagnosticTestPreset SelectedTest
    {
        get => _test;
        set
        {
            if (!CanSelect || value == null || value == _test || !Presets.Contains(value)) return;
            _test = value; Changed(); Changed(nameof(IsTcp)); Changed(nameof(PortError)); Changed(nameof(CanRun));
            Results.Clear(); TcpResult = null;
            _attempts = _replies = _minRtt = _maxRtt = 0; _totalRtt = 0; UpdateSummary();
            Status = Loc.Get("NetworkTests.Ready");
        }
    }
    public bool ShowPresetCards { get; internal set; }
    public bool IsTcp => _test.Kind == DiagnosticTestKind.TcpPort;
    public string Port { get => _port; set { if (!CanSelect) return; _port = value; Changed(); Changed(nameof(PortError)); Changed(nameof(CanRun)); } }
    private bool ValidPort => int.TryParse(_port, NumberStyles.None, CultureInfo.InvariantCulture, out var port) && port is >= 1 and <= 65535;
    public string PortError => IsTcp && !ValidPort ? Loc.Get("NetworkTests.PortError") : "";
    public DiagnosticTcpResult? TcpResult { get => _tcpResult; private set { _tcpResult = value; Changed(); } }
    public long AttemptCount => _attempts;
    public long ReplyCount => _replies;
    public string Summary => _replies == 0 ? Loc.Format("NetworkTests.NoReplies", _attempts)
        : Loc.Format("NetworkTests.Summary", _replies, _attempts, 100d * (_attempts - _replies) / _attempts,
            _minRtt, _maxRtt, _totalRtt / _replies);
    private Guid? PreferredAdapterId { get; set; }
    public int ProbeCount { get; set; } = 1;
    internal AdapterDiagnosticsViewModel CreatePeer() => new(_reader, _ping, _hide, _saveFilter, _tcp, _delay)
        { PreferredAdapterId = _selected?.Id ?? PreferredAdapterId, Target = Target, Port = Port,
            SelectedTest = SelectedTest, ProbeCount = 4, ShowPresetCards = true };
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
    public bool CanRun => CanPing && (!IsTcp || (_tcp != null && ValidPort));
    public bool CanStop => !_disposed && _pingCancellation != null && !_pingCancellation.IsCancellationRequested;
    public bool IsTestRunning => _pingCancellation != null;
    public string SectionTitle => Loc.Get(_pingCancellation != null ? "NetworkTests.RunningTitle" : "Diagnostics.Title");
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
    public ICommand RunTestCommand { get; }
    public ICommand QuickPortCommand { get; }
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
    public Task PingAsync(int count = 1) => count is >= 1 and <= 4 ? RunAsync(count, false) : Task.CompletedTask;
    public Task RunSelectedTestAsync() => !CanRun ? Task.CompletedTask : RunAsync(_test.Kind switch
    {
        DiagnosticTestKind.PingFour => 4,
        DiagnosticTestKind.ContinuousPing => null,
        _ => 1
    }, IsTcp);
    private async Task RunAsync(int? count, bool tcp)
    {
        if (!CanPing || (tcp && (_tcp == null || !ValidPort))) return;
        var adapter = _selected!;
        var request = new DiagnosticPingRequest(adapter.Id, adapter.Index, adapter.Mac, adapter.Name, _source!, _target.Trim());
        int port = tcp ? int.Parse(_port, CultureInfo.InvariantCulture) : 0;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _pingCancellation = cancellation;
        SetBusy(true); Results.Clear(); TcpResult = null;
        _attempts = _replies = _minRtt = _maxRtt = 0; _totalRtt = 0; UpdateSummary();
        Status = Loc.Get(tcp ? "NetworkTests.Connecting" : "Diagnostics.Pinging");
        try
        {
            if (tcp)
            {
                var result = await Task.Run(() => _tcp!.CheckAsync(request, port, cancellation.Token), cancellation.Token);
                if (_disposed) return;
                TcpResult = result;
                Status = Loc.Get(result.Error.Length == 0 ? $"NetworkTests.Tcp.{result.Observation!.Status}" : $"NetworkTests.Error.{result.Error}");
                return;
            }
            for (long i = 0; !count.HasValue || i < count.Value; i++)
            {
                var result = await Task.Run(() => _ping.SendAsync(request, cancellation.Token), cancellation.Token);
                if (_disposed) return;
                _attempts++;
                if (result.Success)
                {
                    _replies++; var rtt = result.Echo!.Milliseconds; _totalRtt += rtt;
                    _minRtt = _replies == 1 ? rtt : Math.Min(_minRtt, rtt); _maxRtt = Math.Max(_maxRtt, rtt);
                }
                if (Results.Count == 100) Results.RemoveAt(0);
                Results.Add(result);
                UpdateSummary(); Status = Summary;
                if (result.Error.Length != 0 || cancellation.IsCancellationRequested) break;
                if (!count.HasValue || i + 1 < count.Value) await _delay(TimeSpan.FromSeconds(1), cancellation.Token);
            }
            if (!_disposed) Status = cancellation.IsCancellationRequested
                ? Loc.Get("NetworkTests.Stopped") + "\n" + Summary
                : Results.LastOrDefault()?.Error is { Length: > 0 } error
                    ? Loc.Get($"Diagnostics.Error.{error}") + "\n" + Summary : Summary;
        }
        catch (OperationCanceledException) { if (!_disposed) Status = Loc.Get("NetworkTests.Stopped") + (tcp ? "" : "\n" + Summary); }
        finally { _pingCancellation = null; SetBusy(false); }
    }
    private void UpdateSummary() { Changed(nameof(AttemptCount)); Changed(nameof(ReplyCount)); Changed(nameof(Summary)); }
    private void SetBusy(bool value)
    {
        _busy = value;
        Changed(nameof(IsTestRunning)); // Also notify completion of a cancelled in-flight call after disposal.
        if (_disposed) return;
        Changed(nameof(CanSelect)); Changed(nameof(CanPing)); Changed(nameof(CanStop)); Changed(nameof(SectionTitle)); CommandManager.InvalidateRequerySuggested();
    }
    public void Dispose() { if (_disposed) return; _disposed = true; _lifetime.Cancel(); _lifetime.Dispose(); }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new(name));
        if (name == nameof(CanPing)) PropertyChanged?.Invoke(this, new(nameof(CanRun)));
        if (name is nameof(CanPing) or nameof(CanRun) or nameof(CanStop)) CommandManager.InvalidateRequerySuggested();
    }
}
