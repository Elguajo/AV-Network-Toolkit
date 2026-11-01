using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using NetworkProfileManager.Commands;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;

namespace NetworkProfileManager.ViewModels;

public interface IMdnsExport { bool Save(string csv); }
public sealed class MdnsDisplayRow : INotifyPropertyChanged
{
    public MdnsDisplayRow(MdnsService service) => Service = service;
    public MdnsService Service { get; private set; }
    public string State => Service.Stale ? Loc.Get("Mdns.Stale") : Service.Incomplete ? Loc.Get("Mdns.Incomplete") : Loc.Get("Mdns.Advertised");
    public void Update(MdnsService service)
    {
        if (Service == service) return;
        Service = service;
        PropertyChanged?.Invoke(this, new(nameof(Service))); PropertyChanged?.Invoke(this, new(nameof(State)));
    }
    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed class MdnsDiscoveryViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IMdnsInterfaces _interfaces;
    private readonly MdnsDiscoverySession _session;
    private readonly IMdnsExport _export;
    private MdnsInterface? _selected;
    private string _filter = "", _status = "", _notice = "";
    private bool _busy, _disposed;
    private MdnsSnapshot _snapshot;
    public MdnsDiscoveryViewModel(IMdnsInterfaces interfaces, MdnsDiscoverySession session, IMdnsExport export)
    {
        _interfaces = interfaces; _session = session; _export = export; _snapshot = session.Snapshot;
        RefreshCommand = new RelayCommand(async _ => await RefreshAsync(), _ => CanSelect);
        StartCommand = new RelayCommand(async _ => await StartAsync(), _ => CanSelect && SelectedInterface != null);
        StopCommand = new RelayCommand(async _ => await StopAsync(), _ => !_disposed && _session.IsActive);
        ExportCommand = new RelayCommand(_ => Export(), _ => !_disposed && Rows.Count != 0);
        Poll();
    }
    public IReadOnlyList<MdnsInterface> Interfaces { get; private set; } = Array.Empty<MdnsInterface>();
    public ObservableCollection<MdnsDisplayRow> Rows { get; } = new();
    public MdnsInterface? SelectedInterface
    {
        get => _selected;
        set { if (!CanSelect) return; _selected = value; Changed(); }
    }
    public bool CanSelect => !_disposed && !_busy && !_session.IsActive;
    public bool IsListening => !_disposed && _session.IsActive && _snapshot.State == MdnsState.Listening;
    public string Scope => _snapshot.Scope?.Display ?? Loc.Get("Mdns.NoScope");
    public string Status { get => _status; private set { _status = value; Changed(); } }
    public string Notice { get => _notice; private set { _notice = value; Changed(); Changed(nameof(HasNotice)); } }
    public bool HasNotice => Notice.Length != 0;
    public string Counters => Loc.Format("Mdns.Counters", _snapshot.Received, _snapshot.Rejected, _snapshot.Limited, Rows.Count);
    public string Filter { get => _filter; set { _filter = value.Length > 256 ? value.Substring(0, 256) : value; Changed(); UpdateRows(); } }
    public ICommand RefreshCommand { get; }
    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand ExportCommand { get; }
    public async Task RefreshAsync()
    {
        if (!CanSelect) return;
        Notice = "";
        _busy = true; Changed(nameof(CanSelect));
        SelectedReset();
        try
        {
            var interfaces = await Task.Run(_interfaces.Read);
            if (!_disposed) { Interfaces = interfaces; Changed(nameof(Interfaces)); Status = Loc.Get("Mdns.Select"); }
        }
        catch { if (!_disposed) Notice = Loc.Get("Mdns.Failed"); }
        finally { _busy = false; Changed(nameof(CanSelect)); CommandManager.InvalidateRequerySuggested(); }
    }
    public async Task StartAsync()
    {
        if (!CanSelect || _selected == null) return;
        Notice = "";
        _busy = true; Changed(nameof(CanSelect));
        try { await _session.StartAsync(_selected); }
        catch { Notice = Loc.Get("Mdns.Failed"); }
        finally { _busy = false; Poll(); }
    }
    public async Task StopAsync()
    { await _session.StopAsync(); if (!_disposed) Poll(); }
    public void Poll()
    {
        if (_disposed) return;
        _snapshot = _session.Snapshot;
        Status = _snapshot.State switch
        {
            MdnsState.Idle => Loc.Get("Mdns.Select"), MdnsState.Starting => Loc.Get("Mdns.Starting"),
            MdnsState.Listening => Loc.Get("Mdns.Listening"), MdnsState.Stopped => Loc.Get("Mdns.Stopped"),
            MdnsState.InterfaceChanged => Loc.Get("Mdns.Changed"), MdnsState.TimedOut => Loc.Get("Mdns.Timeout"),
            _ => Loc.Get("Mdns.Failed")
        };
        if (_snapshot.State == MdnsState.InterfaceChanged) SelectedReset();
        UpdateRows(); Changed(nameof(Scope)); Changed(nameof(CanSelect)); Changed(nameof(IsListening));
        CommandManager.InvalidateRequerySuggested();
    }
    private void SelectedReset() { _selected = null; Changed(nameof(SelectedInterface)); }
    private void UpdateRows()
    {
        var desired = MdnsCsv.Filter(_snapshot.Services, _filter);
        var names = desired.Select(r => r.Instance).ToHashSet(StringComparer.Ordinal);
        for (int i = Rows.Count - 1; i >= 0; i--) if (!names.Contains(Rows[i].Service.Instance)) Rows.RemoveAt(i);
        for (int i = 0; i < desired.Count; i++)
        {
            var existing = Rows.FirstOrDefault(r => r.Service.Instance == desired[i].Instance);
            if (existing == null) Rows.Insert(i, new MdnsDisplayRow(desired[i]));
            else { int old = Rows.IndexOf(existing); if (old != i) Rows.Move(old, i); existing.Update(desired[i]); }
        }
        Changed(nameof(Counters));
    }
    private void Export()
    {
        if (_disposed || Rows.Count == 0) return;
        try { if (_export.Save(MdnsCsv.Export(_snapshot, Rows.Select(r => r.Service)))) Notice = Loc.Get("Mdns.Exported"); }
        catch { Notice = Loc.Get("Mdns.ExportFailed"); }
    }
    public void Dispose() { _disposed = true; _session.Dispose(); Changed(nameof(IsListening)); }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}
