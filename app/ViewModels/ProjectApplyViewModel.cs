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

public interface IProjectApplyDialogs { bool Confirm(string review, bool recovery); }

public sealed class ProjectApplyViewModel : INotifyPropertyChanged
{
    private readonly ProjectApplyCoordinator _coordinator;
    private readonly Func<CancellationToken, Task<IReadOnlyList<ApplyAdapterIdentity>>> _enumerate;
    private readonly Func<IReadOnlyList<ProjectApplyRequest>> _readJournals;
    private readonly Func<int, IReadOnlyList<ProjectApplyRequest>>? _readPage;
    private readonly Func<bool>? _hasMore;
    private readonly Func<int>? _unreadable;
    private int _snapshotOffset;
    private readonly IProjectApplyDialogs _dialogs;
    private readonly ApplyIntent? _intent;
    private readonly ProjectDevice? _device;
    private readonly Guid? _targetAdapterId;
    private ProjectApplyPlan? _plan;
    private ApplyAdapterSnapshot? _recoveryTarget;
    private CancellationTokenSource? _cancellation;
    private ApplyAdapterIdentity? _adapter;
    private ProjectApplyRequest? _saved;
    private bool _busy;
    private string _review = "", _status = "", _observed = "";
    private string _linkLocalAddress = "";
    private string _linkLocalDns1 = "", _linkLocalDns2 = "";
    public ProjectApplyViewModel(ProjectProfile? profile, ProjectDevice? device,
        IProjectApplyBoundary boundary, IProjectApplyJournal journal, IBrowserLauncher browser,
        Func<CancellationToken, Task<IReadOnlyList<ApplyAdapterIdentity>>> enumerate,
        Func<IReadOnlyList<ProjectApplyRequest>> readJournals, IProjectApplyDialogs dialogs,
        int verificationAttempts = 10, string projectName = "", bool linkLocalTool = false, Guid? targetAdapterId = null,
        Func<int, IReadOnlyList<ProjectApplyRequest>>? readPage = null, Func<bool>? hasMore = null, Func<int>? unreadable = null)
    {
        _intent = profile == null ? null : ProjectApplyPolicy.FromProfile(profile);
        _device = device;
        _targetAdapterId = targetAdapterId;
        _enumerate = enumerate; _readJournals = readJournals; _dialogs = dialogs;
        _readPage = readPage; _hasMore = hasMore; _unreadable = unreadable;
        _coordinator = new(boundary, journal, browser, verificationAttempts: verificationAttempts);
        ProfileName = profile?.Name ?? "";
        ProjectName = projectName;
        IsLinkLocalTool = linkLocalTool;
        RefreshCommand = new RelayCommand(async _ => await RefreshAsync(), _ => !IsBusy);
        ReviewCommand = new RelayCommand(async _ => await ReviewAsync(false), _ => !IsBusy && SelectedAdapter != null
            && (_intent != null || IsLinkLocalTool && LinkLocalAddressing.IsCandidate(LinkLocalAddress)));
        GenerateAddressCommand = new RelayCommand(_ => LinkLocalAddress = LinkLocalAddressing.GenerateCandidate(), _ => !IsBusy && IsLinkLocalTool);
        ReviewDhcpCommand = new RelayCommand(async _ => await ReviewDhcpAsync(), _ => !IsBusy && IsLinkLocalTool && SelectedAdapter != null);
        ReviewRecoveryCommand = new RelayCommand(async _ => await ReviewAsync(true), _ => !IsBusy && (_recoveryTarget != null || SelectedSnapshot != null));
        ApplyCommand = new RelayCommand(async _ => await ApplyAsync(), _ => !IsBusy && _plan != null);
        CancelCommand = new RelayCommand(_ => Cancel(), _ => IsBusy || _plan != null);
        ReadSnapshotsCommand = new RelayCommand(_ => LoadSnapshots(false), _ => !IsBusy);
        NextSnapshotsCommand = new RelayCommand(_ => LoadSnapshots(true), _ => !IsBusy && HasMoreSnapshots);
    }
    public string ProfileName { get; }
    public bool IsLinkLocalTool { get; }
    public bool IsProjectAction => !IsLinkLocalTool;
    public string WindowTitle => Loc.Get(IsLinkLocalTool ? "LinkLocal.Title" : _device == null && _intent != null ? "Workspace.ApplyProfile" : "Apply.Title");
    public string LinkLocalAddress
    {
        get => _linkLocalAddress;
        set { if (IsBusy) return; DiscardPlan(); _linkLocalAddress = value; Changed(); }
    }
    public string LinkLocalDns1
    {
        get => _linkLocalDns1;
        set { if (IsBusy) return; DiscardPlan(); _linkLocalDns1 = value; Changed(); }
    }
    public string LinkLocalDns2
    {
        get => _linkLocalDns2;
        set { if (IsBusy) return; DiscardPlan(); _linkLocalDns2 = value; Changed(); }
    }
    public string ProjectName { get; }
    public string DeviceName => _device?.Name ?? "";
    public string Destination => _device?.WebUrl ?? "";
    public ObservableCollection<ApplyAdapterIdentity> Adapters { get; } = new();
    public ObservableCollection<ProjectApplyRequest> Snapshots { get; } = new();
    public ApplyAdapterIdentity? SelectedAdapter
    {
        get => _adapter;
        set { if (IsBusy || _targetAdapterId.HasValue && value?.Id != _targetAdapterId) return; DiscardPlan(); _adapter = value; Changed(); }
    }
    public ProjectApplyRequest? SelectedSnapshot
    {
        get => _saved;
        set { if (IsBusy) return; DiscardPlan(); _saved = value; _recoveryTarget = null;
            Observed = value == null ? "" : Loc.Get("Apply.SavedSnapshot") + "\n" + FormatSnapshot(value.Before); Changed(); }
    }
    public bool IsBusy { get => _busy; private set { _busy = value; Changed(); Changed(nameof(CanSelect)); Changed(nameof(CanSelectAdapter)); } }
    public bool CanSelect => !IsBusy;
    public bool CanSelectAdapter => !IsBusy && !_targetAdapterId.HasValue;
    public string AdapterLabel => Loc.Get(_targetAdapterId.HasValue ? "Ux.TargetAdapter" : "Apply.Adapter");
    public string Review { get => _review; private set { _review = value; Changed(); } }
    public string Status { get => _status; private set { _status = value; Changed(); } }
    public string Observed { get => _observed; private set { _observed = value; Changed(); } }
    public ICommand RefreshCommand { get; }
    public ICommand ReviewCommand { get; }
    public ICommand ReviewRecoveryCommand { get; }
    public ICommand ApplyCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ReadSnapshotsCommand { get; }
    public ICommand NextSnapshotsCommand { get; }
    public bool HasMoreSnapshots => _hasMore?.Invoke() == true;
    public ICommand GenerateAddressCommand { get; }
    public ICommand ReviewDhcpCommand { get; }
    public async Task RefreshAsync()
    {
        if (IsBusy) return;
        await RunAsync(async token =>
        {
            DiscardPlan();
            Status = Loc.Get("Ux.Reading");
            _adapter = null; Changed(nameof(SelectedAdapter));
            Adapters.Clear();
            foreach (var adapter in await _enumerate(token)) Adapters.Add(adapter);
            if (_targetAdapterId.HasValue)
            {
                _adapter = Adapters.SingleOrDefault(a => a.Id == _targetAdapterId);
                Changed(nameof(SelectedAdapter));
            }
            Status = _targetAdapterId.HasValue
                ? _adapter == null ? Loc.Get("Workspace.TargetMissing") : Loc.Format("Ux.TargetReady", _adapter.Alias)
                : Loc.Get("Apply.SelectAdapter");
        });
    }
    public Task ReviewAsync(bool recovery) => PrepareReviewAsync(recovery, false);
    public Task ReviewDhcpAsync() => PrepareReviewAsync(false, true);
    private async Task PrepareReviewAsync(bool recovery, bool dhcp)
    {
        if (IsBusy) return;
        await RunAsync(async token =>
        {
            DiscardPlan();
            Status = Loc.Get("Ux.Reading");
            if (recovery && _targetAdapterId.HasValue
                && (_recoveryTarget ?? SelectedSnapshot?.Before)?.Identity.Id != _targetAdapterId)
                throw new InvalidOperationException("Recovery target differs from the selected adapter.");
            _plan = recovery
                ? await _coordinator.PrepareRecoveryAsync(_recoveryTarget ?? SelectedSnapshot?.Before ?? throw new InvalidOperationException(), token)
                : IsLinkLocalTool
                    ? await _coordinator.PrepareManualAsync(SelectedAdapter ?? throw new InvalidOperationException(), dhcp
                        ? new("dhcp", null, 0, null, "automatic", System.Collections.Immutable.ImmutableArray<string>.Empty)
                        : LinkLocalAddressing.Intent(LinkLocalAddress, LinkLocalDns1, LinkLocalDns2), token)
                    : _device == null
                        ? await _coordinator.PrepareProfileAsync(SelectedAdapter ?? throw new InvalidOperationException(), _intent ?? throw new InvalidOperationException(), token)
                        : await _coordinator.PrepareAsync(SelectedAdapter ?? throw new InvalidOperationException(), _intent ?? throw new InvalidOperationException(), Destination, token);
            Review = Loc.Get(recovery ? "Apply.RecoveryReview" : "Apply.RequestReview") + "\n\n"
                + FormatSnapshot(_plan.Request.Before) + "\n\n" + Loc.Get("Apply.Requested") + "\n" + FormatIntent(_plan.Request.Desired);
            if (_plan.Request.Desired.Mode == "link-local") Review += "\n\n" + Loc.Get("LinkLocal.DadWarning");
            if (IsLinkLocalTool && !recovery && dhcp) Review += "\n\n" + Loc.Get("LinkLocal.DhcpWarning");
            if (!recovery && !IsLinkLocalTool) Review += "\n\n" + Destination;
            if (!recovery && _device != null)
            {
                var uri = new Uri(Destination);
                if (uri.Scheme == "http") Review += "\n\n" + Loc.Get("Device.HttpWarning");
                if (_device.Ip != null && !string.Equals(_device.Ip, uri.Host, StringComparison.OrdinalIgnoreCase))
                    Review += "\n\n" + Loc.Get("Device.HostWarning");
            }
            Status = Loc.Get("Apply.Ready");
        });
    }
    public async Task ApplyAsync()
    {
        if (IsBusy || _plan == null) return;
        var plan = _plan;
        if (!_dialogs.Confirm(Review + "\n\n" + Loc.Get("Apply.Disclaimer"), plan.Request.Recovery)) return;
        await RunAsync(async token =>
        {
            _plan = null;
            Status = Loc.Get("Apply.Working");
            if (!plan.Request.Recovery) _recoveryTarget = plan.Request.Before;
            var outcome = await _coordinator.ExecuteAsync(plan, token);
            Status = StateText(outcome.State) + "\n" + Loc.Get("Apply.Stage") + " "
                + Loc.Get($"Apply.Stage.{outcome.Stage}", Loc.Get("Apply.Stage.Unknown"));
            if (outcome.State == ProjectApplyState.Complete && plan.Destination == null && !plan.Request.Recovery)
                Status = Loc.Get(IsLinkLocalTool ? "LinkLocal.Verified" : "Workspace.Verified");
            if (outcome.JournalUpdateFailed) Status += "\n" + Loc.Get("Apply.JournalWarning");
            if (outcome.Stage == "WorkerStillRunning") Status += "\n" + Loc.Get("Apply.WorkerActive");
            if (outcome.MayHaveChanged && outcome.State != ProjectApplyState.Complete) Status += "\n" + Loc.Get("Apply.ManualRecovery");
            if (plan.Request.Recovery && outcome.State == ProjectApplyState.Complete) Status = Loc.Get("Apply.Recovered");
            Observed = outcome.Observed == null ? Loc.Get("Apply.ObservationUnknown") : FormatSnapshot(outcome.Observed);
        });
    }
    public void Cancel()
    {
        if (IsBusy) { _cancellation?.Cancel(); Status = Loc.Get("Apply.Cancelling"); }
        else { DiscardPlan(); Status = Loc.Get("Apply.State.Cancelled"); }
    }
    private void DiscardPlan()
    {
        if (_plan == null) return;
        _coordinator.Discard(_plan); _plan = null; Review = "";
    }
    private void LoadSnapshots(bool next)
    {
        try
        {
            DiscardPlan(); _saved = null; _recoveryTarget = null; Snapshots.Clear();
            _snapshotOffset = next ? checked(_snapshotOffset + ProjectApplyJournal.PageSize) : 0;
            foreach (var request in _readPage?.Invoke(_snapshotOffset) ?? _readJournals())
                if (!_targetAdapterId.HasValue || request.Before.Identity.Id == _targetAdapterId) Snapshots.Add(request);
            Changed(nameof(SelectedSnapshot));
            Status = Loc.Get(Snapshots.Count == 0 ? "Ux.NoRecovery" : "Apply.SnapshotsRead");
            if (_unreadable?.Invoke() is int count && count > 0) Status += "\n" + Loc.Format("Storage.UnreadableSnapshots", count);
            if (HasMoreSnapshots) Status += "\n" + Loc.Get("Storage.MoreSnapshots");
            Changed(nameof(HasMoreSnapshots));
        }
        catch { Status = Loc.Get("Apply.SnapshotsError"); }
    }
    private async Task RunAsync(Func<CancellationToken, Task> action)
    {
        IsBusy = true;
        using var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;
        try { await action(cancellation.Token); }
        catch (OperationCanceledException) { Status = Loc.Get("Apply.State.Cancelled"); }
        catch { Status = Loc.Get(IsLinkLocalTool ? "LinkLocal.Unsupported" : "Apply.Unsupported"); }
        finally { _cancellation = null; IsBusy = false; }
    }
    public static string FormatSnapshot(ApplyAdapterSnapshot s) =>
        Loc.Get("Apply.Adapter") + ": " + s.Identity.Alias + "\n" + s.Identity.Id + " / " + s.Identity.InterfaceIndex + " / " + s.Identity.PhysicalAddress
        + "\n" + Loc.Get("Apply.Observed") + "\n" + (s.Addresses.Any(a => LinkLocalAddressing.IsLinkLocal(a.Address))
            ? Loc.Get(s.Dhcp ? "LinkLocal.Automatic" : s.Addresses.All(a => a.Origin == "manual")
                ? "LinkLocal.Manual" : "LinkLocal.UnknownOrigin") : s.Dhcp ? "DHCP" : "Static IPv4")
        + "\n" + string.Join("\n", s.Addresses.Select(a => a.Address + "/" + a.PrefixLength + " (" + a.Origin + ", " + a.State + ")"))
        + "\nDNS: " + s.DnsSource + " " + string.Join(", ", s.DnsServers)
        + "\n" + Loc.Get("Apply.DnsRegistration") + " " + s.DnsRegistration
        + (s.Dad == null ? "" : "\n" + Loc.Get("LinkLocal.DadObserved") + " " + s.Dad.Transmits + " / " + s.Dad.Connected)
        + "\n" + Loc.Get("Apply.Persistent") + " " + string.Join(", ", s.PersistentAddresses.Select(a => a.Address + "/" + a.PrefixLength))
        + "\n" + Loc.Get("Apply.Routes") + "\n" + string.Join("\n", s.Routes.Select(r => r.Destination + " → " + r.NextHop + " (" + r.Metric + ", " + r.Store + ")"));
    internal static string FormatIntent(ApplyIntent d) => (d.Mode == "dhcp" ? "DHCP" : d.Address + "/" + d.PrefixLength)
        + "\n" + Loc.Get("EditBar.Gateway") + ": " + (d.Mode == "dhcp" ? Loc.Get("Project.DnsAutomatic") : d.Gateway ?? Loc.Get("Apply.None"))
        + "\nDNS: " + d.DnsMode + " " + string.Join(", ", d.DnsServers);
    internal static string StateText(ProjectApplyState state) => state switch
    {
        ProjectApplyState.Complete => Loc.Get("Apply.State.Complete"),
        ProjectApplyState.Cancelled => Loc.Get("Apply.State.Cancelled"),
        ProjectApplyState.Rejected => Loc.Get("Apply.State.Rejected"),
        ProjectApplyState.ElevationDenied => Loc.Get("Apply.State.ElevationDenied"),
        ProjectApplyState.Unverified => Loc.Get("Apply.State.Unverified"),
        ProjectApplyState.BrowserFailed => Loc.Get("Apply.State.BrowserFailed"),
        ProjectApplyState.JournalFailed => Loc.Get("Apply.State.JournalFailed"),
        _ => Loc.Get("Apply.State.ApplyFailed")
    };
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed([CallerMemberName] string? name = null)
    { PropertyChanged?.Invoke(this, new(name)); CommandManager.InvalidateRequerySuggested(); }
}
