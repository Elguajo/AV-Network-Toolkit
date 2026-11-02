using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NetworkProfileManager.Commands;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Services;

namespace NetworkProfileManager.ViewModels;

internal sealed class ApplicationUpdateViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ApplicationUpdateService _service;
    private readonly ApplicationUpdatePreferences _preferences;
    private readonly string? _key;
    private readonly bool _installed;
    private readonly Func<string?> _chooseFolder;
    private readonly Func<bool, Task<bool>> _prepareExit;
    private readonly Action _exit;
    private readonly Func<string, bool> _confirm;
    private CancellationTokenSource? _cancellation;
    private ApplicationUpdateOffer? _offer;
    private DownloadedApplicationUpdate? _download;
    private bool _busy, _disposed, _portableReady;
    private string _status = "", _details = "";
    private double _progress;
    internal ApplicationUpdateViewModel(ApplicationUpdateService service, ApplicationUpdatePreferences preferences, string? key,
        bool installed, Func<string?> chooseFolder, Func<bool, Task<bool>> prepareExit, Action exit, Func<string, bool> confirm)
    {
        _service = service; _preferences = preferences; _key = key; _installed = installed;
        _chooseFolder = chooseFolder; _prepareExit = prepareExit; _exit = exit; _confirm = confirm;
        CheckCommand = new RelayCommand(async _ => await CheckAsync(), _ => !IsBusy);
        DownloadCommand = new RelayCommand(async _ => await DownloadAsync(), _ => CanDownload);
        InstallCommand = new RelayCommand(async _ => await InstallAsync(), _ => CanInstall);
        FinishCommand = new RelayCommand(async _ => await RunAsync(async _ =>
        {
            using var barrier = NetworkMutationLease.TrySuspendForUpdate();
            if (barrier != null && await _prepareExit(false)) _exit(); else Status = Loc.Get("Updates.ExitBlocked");
        }), _ => CanFinish);
        CancelCommand = new RelayCommand(_ => Cancel(), _ => IsBusy);
        Status = key == null ? Loc.Get("Updates.NotConfigured") : Loc.Get("Updates.Ready");
        if (_preferences.IsBlocked) Status = Loc.Get("Updates.PreferenceError");
    }
    public ICommand CheckCommand { get; }
    public ICommand DownloadCommand { get; }
    public ICommand InstallCommand { get; }
    public ICommand FinishCommand { get; }
    public ICommand CancelCommand { get; }
    public string CurrentVersion => $"{ApplicationUpdateTrust.CurrentVersion.ToString(3)} — {ApplicationUpdateTrust.BuildChannel}";
    public string PackageKind => Loc.Get(_installed ? "Updates.Installed" : "Updates.Portable");
    public string InstallLabel => Loc.Get(_installed ? "Updates.Install" : "Updates.Extract");
    public bool IsBusy { get => _busy; private set { _busy = value; Changed(); Changed(nameof(CanConfigure)); Changed(nameof(CanDownload)); Changed(nameof(CanInstall)); CommandManager.InvalidateRequerySuggested(); } }
    public bool CanConfigure => !IsBusy && !_preferences.IsBlocked;
    public bool CanDownload => !IsBusy && _offer != null && _download == null;
    public bool CanInstall => !IsBusy && _download != null && !_portableReady;
    public bool CanFinish => !IsBusy && _portableReady;
    public string Status { get => _status; private set { _status = value; Changed(); } }
    public string Details { get => _details; private set { _details = value; Changed(); } }
    public double Progress { get => _progress; private set { _progress = value; Changed(); } }
    public bool AutomaticChecks
    {
        get => _preferences.Current.AutomaticChecks;
        set { if (IsBusy) return; Save(_preferences.Current with { AutomaticChecks = value }); Changed(); }
    }
    public bool IncludePreview
    {
        get => _preferences.Current.IncludePreview;
        set { if (IsBusy) return; Save(_preferences.Current with { IncludePreview = value }); _offer = null; _download = null; Details = ""; Changed(); Changed(nameof(CanDownload)); Changed(nameof(CanInstall)); CommandManager.InvalidateRequerySuggested(); }
    }
    private void Save(ApplicationUpdatePreference preference)
    { if (!_preferences.Save(preference)) Status = Loc.Get("Updates.PreferenceError"); Changed(nameof(CanConfigure)); }
    private string? _notifiedVersion;
    internal event Action? UpdateAvailable;
    internal async Task CheckAutomaticallyAsync(DateTimeOffset now)
    {
        if (_key == null || !_preferences.IsDue(now) || IsBusy) return;
        await CheckAsync();
        if (_offer != null && _notifiedVersion != _offer.Manifest.Version)
        { _notifiedVersion = _offer.Manifest.Version; UpdateAvailable?.Invoke(); }
    }
    internal async Task CheckAsync()
    {
        if (IsBusy || _disposed) return;
        _offer = null; _download = null; _portableReady = false; Details = "";
        if (_key == null) { Status = Loc.Get("Updates.NotConfigured"); return; }
        await RunAsync(async token =>
        {
            Status = Loc.Get("Updates.Checking");
            Save(_preferences.Current with { LastCheck = DateTimeOffset.UtcNow });
            if (!Environment.Is64BitProcess) throw new InvalidDataException("Updates require the supported win-x64 package.");
            _offer = await _service.CheckAsync(ApplicationUpdateTrust.CurrentVersion, IncludePreview, Environment.OSVersion.Version.Build, token);
            Status = Loc.Get(_offer == null ? "Updates.NoUpdate" : "Updates.Available");
            if (_offer != null)
            {
                var m = _offer.Manifest; var package = _installed ? m.Installer : m.Portable;
                Details = $"{m.Version} — {m.Channel}\n{package.Size / 1048576d:F1} MB\nSource: {m.SourceCommit}\n\n{_offer.Notes}";
            }
        });
    }
    internal async Task DownloadAsync()
    {
        if (!CanDownload || _disposed) return;
        var offer = _offer!;
        await RunAsync(async token =>
        {
            Status = Loc.Get("Updates.Downloading"); Progress = 0;
            _download = await _service.DownloadAsync(offer, _installed, new Progress<double>(v => { if (!_disposed && _cancellation?.Token == token) Progress = v; }), token);
            Progress = 100;
            Status = Loc.Get("Updates.Verified");
        });
    }
    internal async Task InstallAsync()
    {
        if (!CanInstall || _disposed || !_confirm(Loc.Get(_installed ? "Updates.InstallConsent" : "Updates.PortableConsent"))) return;
        var download = _download!;
        string? destination = null;
        if (!_installed)
        {
            var parent = _chooseFolder(); if (parent == null) return;
            destination = Path.Combine(parent, "AVNetworkToolkit-" + download.Offer.Manifest.Version);
        }
        await RunAsync(async token =>
        {
            if (!_installed)
            {
                Status = Loc.Get("Updates.Extracting");
                var exe = await Task.Run(() => ApplicationUpdateService.ExtractPortable(download, destination!, _key!, token), token);
                _portableReady = true; Details = exe; Status = Loc.Get("Updates.PortableReady"); Changed(nameof(CanFinish)); return;
            }
            using var barrier = NetworkMutationLease.TrySuspendForUpdate();
            if (barrier == null || !await _prepareExit(true)) { Status = Loc.Get("Updates.ExitBlocked"); return; }
            Status = Loc.Get("Updates.PreparingExit");
            await ApplicationUpdateHandoff.PrepareAsync(download, token);
            _exit();
        });
    }
    private async Task RunAsync(Func<CancellationToken, Task> action)
    {
        if (IsBusy || _disposed) return;
        IsBusy = true; using var cancellation = new CancellationTokenSource(); _cancellation = cancellation;
        try { await action(cancellation.Token); }
        catch (OperationCanceledException) { Status = Loc.Get("Updates.Canceled"); }
        catch (Exception e)
        { Status = Loc.Get("Updates.Failed") + " " + e.Message; }
        finally { _cancellation = null; IsBusy = false; Changed(nameof(CanFinish)); }
    }
    internal void Cancel() => _cancellation?.Cancel();
    public void Dispose() { _disposed = true; Cancel(); }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
