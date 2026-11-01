using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Data;
using NetworkProfileManager.Commands;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;

namespace NetworkProfileManager.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly NetworkService _networkSvc = new NetworkService();
        private readonly ProfileRepository _profileRepo = new ProfileRepository();
        private readonly HistoryRepository _historyRepo = new HistoryRepository();
        private readonly Func<List<AdapterInfo>> _getAdapters;
        private readonly LocalProfileApplyService _apply;
        private System.Threading.CancellationTokenSource? _applyCancellation;
        public ProjectApplyOutcome? LastApplyOutcome { get; private set; }
        public bool ConfirmApplicationExit()
        {
            if (!IsLoading) return true;
            _applyCancellation?.Cancel(); return false;
        }

        private AdapterViewModel? _selectedAdapter;
        private bool _isLoading;
        private bool _hasRefreshed;

        public ObservableCollection<AdapterViewModel> Adapters { get; } = new ObservableCollection<AdapterViewModel>();
        public ICollectionView VisibleAdapters { get; }
        private bool _showAllAdapters;
        public bool ShowAllAdapters
        {
            get => _showAllAdapters;
            set
            {
                if (_showAllAdapters == value) return;
                _showAllAdapters = value;
                if (!value && SelectedAdapter?.Info.IsSystemInterface == true) SelectedAdapter = null;
                VisibleAdapters.Refresh();
                OnPropertyChanged();
            }
        }
        public AdapterDiagnosticsViewModel Diagnostics { get; }

        /// <summary>
        /// Set by the view to present a Yes/No confirmation dialog for destructive
        /// actions. Network changes fail closed when no handler is installed.
        /// The existing non-network delete helper retains its prior default.
        /// </summary>
        public Func<string, string, Task<bool>>? ConfirmHandler { get; set; }

        public const int MaxProfilesPerAdapter = 5;

        public AdapterViewModel? SelectedAdapter
        {
            get => _selectedAdapter;
            set
            {
                // Refresh() re-selects the same adapter after every apply; that is
                // not a change and must not reset the page (scroll, fade).
                if (ReferenceEquals(_selectedAdapter, value)) return;
                if (_selectedAdapter != null) _selectedAdapter.PropertyChanged -= SelectedAdapterChanged;
                _selectedAdapter = value;
                if (_selectedAdapter != null) _selectedAdapter.PropertyChanged += SelectedAdapterChanged;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ApplyTargetLabel));
            }
        }

        public string StorageError { get; private set; } = "";

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public string ApplyTargetLabel => Helpers.Loc.Format("Workspace.ApplyTo", SelectedAdapter?.Name ?? "—");
        private void SelectedAdapterChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AdapterViewModel.Name)) OnPropertyChanged(nameof(ApplyTargetLabel));
        }

        public ICommand RefreshCommand { get; }
        public ICommand ApplyCommand { get; }
        public ICommand PingCommand { get; }
        public ICommand ApplyProfileCommand { get; }
        public ICommand DeleteProfileCommand { get; }
        public ICommand RevertCommand { get; }
        public ICommand ClearHistoryCommand { get; }
        public ICommand ApplyPresetCommand { get; }
        public ICommand ExportProfilesCommand { get; }
        public ICommand ImportProfilesCommand { get; }

        /// <summary>Vendor subnet shortcuts shown in the edit bar's preset menu.</summary>
        public IReadOnlyList<VendorPreset> VendorPresets => VendorPreset.All;

        public MainViewModel() : this(null, AdapterDiagnosticsViewModel.Create()) { }

        internal MainViewModel(Func<List<AdapterInfo>>? getAdapters, AdapterDiagnosticsViewModel? diagnostics = null, LocalProfileApplyService? apply = null)
        {
            VisibleAdapters = new ListCollectionView(Adapters)
            { Filter = item => ShowAllAdapters || !((AdapterViewModel)item).Info.IsSystemInterface };
            _apply = apply ?? LocalProfileApplyService.Create();
            Diagnostics = diagnostics ?? AdapterDiagnosticsViewModel.Create(persist: false);
            _getAdapters = getAdapters ?? _networkSvc.GetAdapters;
            RefreshCommand = new RelayCommand(_ => Refresh());
            ApplyCommand = new RelayCommand(
                async _ => await ApplyAsync(),
                _ => SelectedAdapter != null && SelectedAdapter.IsEditValid && !IsLoading);
            PingCommand = new RelayCommand(
                async _ => await DoPingAsync(),
                _ => Diagnostics.CanPing);
            ApplyProfileCommand = new RelayCommand(
                async p => await ApplyProfileAsync(p as ProfileViewModel),
                p => p is ProfileViewModel && !IsLoading);
            DeleteProfileCommand = new RelayCommand(
                async p => await DeleteProfileAsync(p as ProfileViewModel),
                p => p is ProfileViewModel && !IsLoading);
            RevertCommand = new RelayCommand(
                async p => await RevertAsync(p as HistoryEntry),
                p => p is HistoryEntry h && (h.WasDhcp || !string.IsNullOrEmpty(h.OldIp)) && !IsLoading);
            ClearHistoryCommand = new RelayCommand(
                async _ => await ClearHistoryAsync(),
                _ => SelectedAdapter != null && !IsLoading);
            ApplyPresetCommand = new RelayCommand(
                p => ApplyPreset(p as VendorPreset),
                _ => SelectedAdapter != null && !IsLoading);
            ExportProfilesCommand = new RelayCommand(
                _ => ExportProfiles(),
                _ => SelectedAdapter != null && SelectedAdapter.Profiles.Count > 0);
            ImportProfilesCommand = new RelayCommand(
                _ => ImportProfiles(),
                _ => SelectedAdapter != null && !IsLoading);

            Refresh();
        }

        // ── Refresh ─────────────────────────────────────────────────
        public void Refresh()
        {
            var selectedId = _selectedAdapter?.Info.Id;
            var adapters = _getAdapters();

            foreach (var info in adapters)
            {
                var existing = Adapters.FirstOrDefault(a => a.Info.Id == info.Id);
                if (existing != null)
                {
                    existing.UpdateInfo(info);
                    if (!IsLoading)
                    {
                        try
                        {
                            var saved = _profileRepo.Load(info.Id);
                            existing.Profiles.Clear();
                            foreach (var p in saved) existing.Profiles.Add(new ProfileViewModel(p));
                            var history = _historyRepo.Load(info.Id);
                            existing.History.Clear(); foreach (var h in history) existing.History.Add(h);
                        }
                        catch (Exception e) when (e is System.IO.IOException or UnauthorizedAccessException)
                        { StorageError = Loc.Get("Storage.ReadFailed"); NotificationService.Error(StorageError); }
                    }
                }
                else
                {
                    var profiles = new List<ProfileViewModel>();
                    var history = new List<HistoryEntry>();
                    try { profiles = _profileRepo.Load(info.Id).Select(p => new ProfileViewModel(p)).ToList(); }
                    catch (Exception e) when (e is System.IO.IOException or UnauthorizedAccessException)
                    { StorageError = Loc.Get("Storage.ReadFailed"); NotificationService.Error(StorageError); }
                    try { history = _historyRepo.Load(info.Id); }
                    catch (Exception e) when (e is System.IO.IOException or UnauthorizedAccessException)
                    { StorageError = Loc.Get("Storage.ReadFailed"); NotificationService.Error(StorageError); }

                    var vm = new AdapterViewModel(
                        info,
                        new ObservableCollection<ProfileViewModel>(profiles),
                        new ObservableCollection<HistoryEntry>(history));
                    Adapters.Add(vm);
                }
            }

            var ids = adapters.Select(a => a.Id).ToHashSet();
            for (int i = Adapters.Count - 1; i >= 0; i--)
                if (!ids.Contains(Adapters[i].Info.Id))
                    Adapters.RemoveAt(i);

            if (SelectedAdapter?.Info.IsSystemInterface == true && !ShowAllAdapters) SelectedAdapter = null;
            VisibleAdapters.Refresh();
            SelectedAdapter = !_hasRefreshed && selectedId == null
                ? VisibleAdapters.Cast<AdapterViewModel>().FirstOrDefault()
                : VisibleAdapters.Cast<AdapterViewModel>().FirstOrDefault(a => a.Info.Id == selectedId);
            _hasRefreshed = true;

        }

        /// <summary>
        /// Shared apply pipeline. Captures the target adapter as a local so a
        /// mid-apply selection change cannot redirect the typed request or its history.
        /// </summary>
        private async Task ApplyCoreAsync(AdapterViewModel adapter, IpProfile profile, HistoryEntry? recovery = null)
        {
            if (IsLoading) return;
            IsLoading = true; LastApplyOutcome = null;
            using var cancellation = new System.Threading.CancellationTokenSource();
            _applyCancellation = cancellation;
            string adapterId = adapter.Info.Id, profileName = profile.Name;
            try
            {
                var result = await _apply.RunAsync(adapterId, profile, recovery,
                    review => ConfirmHandler?.Invoke(Loc.Get(recovery == null ? "Apply.RequestReview" : "Apply.Recovery"), review)
                        ?? Task.FromResult(false), cancellation.Token);
                if (result.Outcome == null) return;
                LastApplyOutcome = result.Outcome;
                if (result.Outcome.State == ProjectApplyState.Complete)
                {
                    bool historyFailed = false;
                    if (recovery == null)
                    {
                        var before = result.Request.Before;
                        var address = before.Addresses.Single();
                        int mask = unchecked((int)(uint.MaxValue << (32 - address.PrefixLength)));
                        var entry = new HistoryEntry
                        {
                            AdapterId = before.Identity.Id.ToString(), AdapterName = before.Identity.Alias,
                            OldIp = address.Address, OldSubnet = new System.Net.IPAddress(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(mask))).ToString(),
                            OldGateway = before.Routes.SingleOrDefault(r => r.Store == "active" && r.Destination == "0.0.0.0/0")?.NextHop ?? "",
                            NewIp = result.Request.Desired.Mode == "dhcp" ? "DHCP" : result.Request.Desired.Address!,
                            WasDhcp = before.Dhcp, IsDhcp = result.Request.Desired.Mode == "dhcp", ProfileName = profileName,
                            RecoveryOperationId = result.Request.OperationId
                        };
                        try { _historyRepo.Append(adapterId, entry); adapter.History.Insert(0, entry); }
                        catch (Exception e) when (e is System.IO.IOException or UnauthorizedAccessException)
                        { historyFailed = true; }
                    }
                    if (historyFailed || result.Outcome.JournalUpdateFailed)
                        NotificationService.Warning(Loc.Get("LocalApply.HistoryFailed"));
                    else NotificationService.Success(Loc.Get(recovery == null ? "LocalApply.Verified" : recovery.RecoveryOperationId == null ? "LocalApply.AddressOnlyRestored" : "Apply.Recovered"));
                }
                else NotificationService.Error(ProjectApplyViewModel.StateText(result.Outcome.State) + "\n" + Loc.Get("Apply.ManualRecovery"));
                Refresh();
            }
            catch (LocalApplyBlockedException error) { NotificationService.Error(Loc.Get(error.Code)); }
            catch (OperationCanceledException) { NotificationService.Info(Loc.Get("Apply.State.Cancelled")); }
            catch { NotificationService.Error(Loc.Get("Apply.Unsupported")); }
            finally { _applyCancellation = null; IsLoading = false; }
        }

        internal static string ErrorText(string? contextKey, string detail)
            => contextKey == null ? detail : Helpers.Loc.Format(contextKey, detail);

        internal Task ApplyAsync()
        {
            var adapter = SelectedAdapter;
            return adapter == null ? Task.CompletedTask : ApplyCoreAsync(adapter, new IpProfile
            {
                IsDhcp = adapter.EditIsDhcp, IpAddress = adapter.EditIp, SubnetMask = adapter.EditSubnet,
                Gateway = adapter.EditGateway, PrimaryDns = adapter.EditPrimaryDns, SecondaryDns = adapter.EditSecondaryDns
            });
        }
        private Task ApplyProfileAsync(ProfileViewModel? profile)
        {
            var adapter = SelectedAdapter;
            return adapter == null || profile == null ? Task.CompletedTask : ApplyCoreAsync(adapter, new IpProfile
            {
                Name = profile.Name, IsDhcp = profile.IsDhcp, IpAddress = profile.IpAddress, SubnetMask = profile.SubnetMask,
                Gateway = profile.Gateway, PrimaryDns = profile.PrimaryDns, SecondaryDns = profile.SecondaryDns
            });
        }

        // ── Delete profile ───────────────────────────────────────────
        private async Task DeleteProfileAsync(ProfileViewModel? pvm)
        {
            var adapter = SelectedAdapter;
            if (pvm == null || adapter == null) return;

            if (!await ConfirmAsync(
                    Helpers.Loc.Get("Confirm.DeleteProfileTitle", "Profili Sil"),
                    Helpers.Loc.Format("Confirm.DeleteProfileMessage", pvm.Name)))
                return;

            if (!SaveProfiles(adapter, adapter.Profiles.Where(p => p != pvm).Select(p => p.Model).ToList())) return;
            adapter.Profiles.Remove(pvm);
            NotificationService.Info(Helpers.Loc.Format("Status.ProfileDeleted", pvm.Name));
        }

        // ── Profile import / export ──────────────────────────────────

        /// <summary>Writes the selected adapter's profiles to a JSON file the user picks.</summary>
        private void ExportProfiles()
        {
            var adapter = SelectedAdapter;
            if (adapter == null || adapter.Profiles.Count == 0) return;

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title      = Helpers.Loc.Get("Profiles.ExportTitle", "Profilleri Dışa Aktar"),
                FileName   = $"profiles_{DateTime.Now:yyyyMMdd}.json",
                DefaultExt = ".json",
                Filter     = Helpers.Loc.Get("Profiles.JsonFilter", "JSON dosyası|*.json")
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                var models = adapter.Profiles.Select(p => p.Model).ToList();
                System.IO.File.WriteAllText(dlg.FileName,
                    Newtonsoft.Json.JsonConvert.SerializeObject(models, Newtonsoft.Json.Formatting.Indented),
                    System.Text.Encoding.UTF8);

                string msg = Helpers.Loc.Format("Status.ProfilesExported", models.Count);
                NotificationService.Success(msg);
            }
            catch (Exception ex)
            {
                NotificationService.Error(ex.Message);
            }
        }

        /// <summary>
        /// Adds profiles from a JSON file. Imported entries are validated the same way
        /// hand-typed ones are — a file is untrusted input, and an invalid address here
        /// would only fail later at netsh with a far less obvious message.
        /// </summary>
        private void ImportProfiles()
        {
            var adapter = SelectedAdapter;
            if (adapter == null) return;

            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title      = Helpers.Loc.Get("Profiles.ImportTitle", "Profilleri İçe Aktar"),
                DefaultExt = ".json",
                Filter     = Helpers.Loc.Get("Profiles.JsonFilter", "JSON dosyası|*.json")
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                var json = System.IO.File.ReadAllText(dlg.FileName);
                var incoming = Newtonsoft.Json.JsonConvert.DeserializeObject<List<IpProfile>>(json);
                if (incoming == null || incoming.Count == 0)
                {
                    NotificationService.Warning(
                        Helpers.Loc.Get("Status.ImportEmpty", "Dosyada profil bulunamadı."));
                    return;
                }

                int added = 0, skipped = 0;
                var candidate = adapter.Profiles.Select(p => p.Model).ToList();
                var additions = new List<IpProfile>();
                foreach (var p in incoming)
                {
                    if (candidate.Count >= MaxProfilesPerAdapter) { skipped++; continue; }
                    if (p == null || string.IsNullOrWhiteSpace(p.Name)) { skipped++; continue; }
                    if (candidate.Any(existing =>
                            string.Equals(existing.Name, p.Name, StringComparison.OrdinalIgnoreCase)))
                    { skipped++; continue; }
                    if (!IsImportedProfileValid(p)) { skipped++; continue; }

                    candidate.Add(p); additions.Add(p);
                    added++;
                }

                if (added > 0)
                {
                    if (!SaveProfiles(adapter, candidate)) return;
                    foreach (var item in additions) adapter.Profiles.Add(new ProfileViewModel(item));
                }

                string msg = Helpers.Loc.Format("Status.ProfilesImported", added, skipped);
                if (added > 0) NotificationService.Success(msg);
                else NotificationService.Warning(msg);
            }
            catch (Exception ex)
            {
                NotificationService.Error(ex.Message);
            }
        }

        private static bool IsImportedProfileValid(IpProfile p)
        {
            if (p.IsDhcp) return true;
            try
            {
                NetworkService.EnsureValidIp(p.IpAddress,  nameof(p.IpAddress));
                NetworkService.EnsureValidIp(p.SubnetMask, nameof(p.SubnetMask));
                if (!string.IsNullOrWhiteSpace(p.Gateway))
                    NetworkService.EnsureValidIp(p.Gateway, nameof(p.Gateway));
                if (!string.IsNullOrWhiteSpace(p.PrimaryDns))
                    NetworkService.EnsureValidIp(p.PrimaryDns, nameof(p.PrimaryDns));
                if (!string.IsNullOrWhiteSpace(p.SecondaryDns))
                    NetworkService.EnsureValidIp(p.SecondaryDns, nameof(p.SecondaryDns));
                return true;
            }
            catch { return false; }
        }

        /// <summary>Ctrl+1..5 — applies the Nth profile of the selected adapter.</summary>
        public async Task ApplyProfileByIndexAsync(int index)
        {
            var adapter = SelectedAdapter;
            if (adapter == null || IsLoading) return;
            if (index < 0 || index >= adapter.Profiles.Count) return;
            await ApplyProfileAsync(adapter.Profiles[index]);
        }

        // ── Vendor preset ────────────────────────────────────────────
        /// <summary>
        /// Fills the edit fields from a preset. Deliberately does NOT apply — the
        /// engineer still reviews the address and presses Apply, so a mis-clicked
        /// preset can never drop the adapter off its current network by itself.
        /// </summary>
        private void ApplyPreset(VendorPreset? preset)
        {
            var adapter = SelectedAdapter;
            if (preset == null || adapter == null) return;

            if (preset.IsDhcp)
            {
                adapter.EditIsDhcp = true;
            }
            else
            {
                adapter.EditIsStatic = true;
                adapter.EditIp       = preset.IpAddress;
                adapter.EditSubnet   = preset.SubnetMask;
                adapter.EditGateway  = preset.Gateway;
            }

        }

        private async Task<bool> ConfirmAsync(string title, string message)
        {
            if (ConfirmHandler == null) return true;
            return await ConfirmHandler(title, message);
        }

        // ── Ping ─────────────────────────────────────────────────────
        private Task DoPingAsync() => Diagnostics.PingAsync();

        // ── Revert ───────────────────────────────────────────────────
        private Task RevertAsync(HistoryEntry? entry)
        {
            var adapter = SelectedAdapter;
            return adapter == null || entry == null ? Task.CompletedTask : ApplyCoreAsync(adapter, new IpProfile(), entry);
        }

        // ── Clear history ────────────────────────────────────────────
        private async Task ClearHistoryAsync()
        {
            var adapter = SelectedAdapter;
            if (adapter == null) return;

            if (!await ConfirmAsync(
                    Helpers.Loc.Get("Confirm.ClearHistoryTitle", "Geçmişi Temizle"),
                    Helpers.Loc.Get("Confirm.ClearHistoryMessage",
                        "Bu adaptörün tüm geçmiş kayıtları silinecek. Devam edilsin mi?")))
                return;

            _historyRepo.Clear(adapter.Info.Id);
            adapter.History.Clear();
            var cleared = Helpers.Loc.Get("Status.HistoryCleared", "Geçmiş temizlendi.");
            NotificationService.Info(cleared);
        }

        // ── Update profile ───────────────────────────────────────────
        public void UpdateProfile(ProfileViewModel pvm, IpProfile updated) => TryUpdateProfile(pvm, updated);
        internal bool TryUpdateProfile(ProfileViewModel pvm, IpProfile updated, AdapterViewModel? target = null)
        {
            var adapter = target ?? SelectedAdapter;
            if (adapter == null || IsLoading) return false;
            int idx = adapter.Profiles.IndexOf(pvm);
            if (idx < 0) return false;
            var candidate = adapter.Profiles.Select(p => p.Model).ToList();
            candidate[idx] = updated;
            if (!SaveProfiles(adapter, candidate)) return false;
            adapter.Profiles[idx] = new ProfileViewModel(updated);
            adapter.RefreshActiveProfiles();
            NotificationService.Success(Loc.Format("Status.ProfileUpdated", updated.Name));
            return true;
        }

        public void AddProfile(IpProfile profile) => TryAddProfile(profile);
        internal bool TryAddProfile(IpProfile profile, AdapterViewModel? target = null)
        {
            var adapter = target ?? SelectedAdapter;
            if (adapter == null || IsLoading) return false;
            if (adapter.Profiles.Count >= MaxProfilesPerAdapter)
            {
                StorageError = Loc.Get("Status.MaxProfilesReached");
                NotificationService.Warning(StorageError); return false;
            }
            var candidate = adapter.Profiles.Select(p => p.Model).ToList(); candidate.Add(profile);
            if (!SaveProfiles(adapter, candidate)) return false;
            adapter.Profiles.Add(new ProfileViewModel(profile));
            NotificationService.Success(Loc.Format("Status.ProfileAdded", profile.Name));
            return true;
        }
        private bool SaveProfiles(AdapterViewModel adapter, List<IpProfile> candidate)
        {
            try { _profileRepo.Save(adapter.Info.Id, candidate); StorageError = ""; return true; }
            catch (Exception e) when (e is System.IO.IOException or UnauthorizedAccessException or InvalidOperationException or Newtonsoft.Json.JsonException)
            {
                StorageError = Loc.Get("Storage.SaveFailed");
                OnPropertyChanged(nameof(StorageError)); NotificationService.Error(StorageError); return false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
