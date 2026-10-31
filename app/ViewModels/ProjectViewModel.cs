using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using NetworkProfileManager.Commands;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;

namespace NetworkProfileManager.ViewModels;

public interface IProjectDialogs
{
    string? OpenPath();
    string? ChooseFolder();
    string? SavePath(string? folder);
    bool ConfirmDiscard();
    bool ConfirmDelete();
    bool ConfirmOverwrite(string path);
    bool ConfirmDeleteDevice();
    bool ConfirmOpenDevice(string destination, bool isHttp, bool hostMismatch, bool unlinked);
}

public sealed partial class ProjectViewModel : INotifyPropertyChanged
{
    private readonly IProjectDialogs _dialogs;
    private readonly ProjectStore _store;
    private NetworkProject _project = Empty();
    private ProjectSnapshot? _snapshot;
    private ProjectProfile? _selected;
    private ProjectProfileEditor _editor = new();
    private bool _documentDirty, _editorDirty;
    private bool _updatingProfiles;
    private string _error = "";
    private string _fileFeedback = "";
    private string? _folder;

    public ProjectViewModel(IProjectDialogs dialogs) : this(dialogs, new ProjectStore(), new DefaultBrowserLauncher()) { }
    public ProjectViewModel(IProjectDialogs dialogs, IBrowserLauncher browser) : this(dialogs, new ProjectStore(), browser) { }
    internal ProjectViewModel(IProjectDialogs dialogs, ProjectStore store, IBrowserLauncher? browser = null)
    {
        (_dialogs, _store) = (dialogs, store);
        AttachEditor();
        NewCommand = new RelayCommand(_ => Run(() => { if (CanDiscard()) Load(Empty(), null); }));
        OpenCommand = new RelayCommand(_ => Run(Open));
        ChooseFolderCommand = new RelayCommand(_ => Run(ChooseFolder));
        SaveAsCommand = new RelayCommand(_ => Run(() => Save(true)));
        SaveCommand = new RelayCommand(_ => Run(() => Save(false)), _ => _snapshot == null || HasUnsavedChanges);
        NewProfileCommand = new RelayCommand(_ => Run(() =>
        {
            if (_editorDirty && !_dialogs.ConfirmDiscard()) return;
            _selected = null;
            Changed(nameof(SelectedProfile));
            SetEditor(new ProjectProfile());
        }));
        KeepProfileCommand = new RelayCommand(_ => Run(KeepProfile));
        DeleteProfileCommand = new RelayCommand(_ => Run(DeleteProfile), _ => _selected != null);
        InitializeDevices(browser ?? new DefaultBrowserLauncher());
    }

    private static NetworkProject Empty() => new()
    {
        Project = new ProjectIdentity { Name = Loc.Get("Project.NewName", "New project") }
    };
    public ObservableCollection<ProjectProfile> Profiles { get; } = new();
    public string ProjectName
    {
        get => _project.Project.Name;
        set { _project = _project with { Project = _project.Project with { Name = value } }; Dirty(); Changed(); }
    }
    public string Description
    {
        get => _project.Project.Description ?? "";
        set { _project = _project with { Project = _project.Project with { Description = value } }; Dirty(); Changed(); }
    }
    public ProjectProfile? SelectedProfile
    {
        get => _selected;
        set
        {
            if (_updatingProfiles) return;
            if (ReferenceEquals(value, _selected)) return;
            if (_editorDirty && !_dialogs.ConfirmDiscard()) { Changed(); return; }
            _selected = value;
            SetEditor(value ?? new ProjectProfile());
            Changed();
        }
    }
    public ProjectProfileEditor Editor => _editor;
    public string FilePath => _snapshot?.Path ?? Loc.Get("Project.Unsaved", "Not saved");
    public string Folder => _folder ?? "";
    public string BackupPath => _snapshot?.BackupPath ?? "";
    public string Error { get => _error; private set { _error = value; Changed(); Changed(nameof(HasFeedback)); } }
    public string FileFeedback { get => _fileFeedback; private set { _fileFeedback = value; Changed(); Changed(nameof(HasFeedback)); } }
    public bool HasFeedback => Error.Length != 0 || FileFeedback.Length != 0 || DeviceNotice.Length != 0;
    public bool HasUnsavedChanges => _documentDirty || _editorDirty || _deviceEditorDirty;
    public ICommand NewCommand { get; }
    public ICommand OpenCommand { get; }
    public ICommand ChooseFolderCommand { get; }
    public ICommand SaveAsCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand NewProfileCommand { get; }
    public ICommand KeepProfileCommand { get; }
    public ICommand DeleteProfileCommand { get; }

    public bool CanDiscard() => !HasUnsavedChanges || _dialogs.ConfirmDiscard();
    internal bool ConfirmDiscardModalDraft() => _dialogs.ConfirmDiscard();

    // Detached modal drafts never mutate the document on Cancel or failed validation.
    public bool TryStoreProfile(ProjectProfileEditor draft)
    {
        var stored = false;
        Run(() =>
        {
            if (_editorDirty) throw new ProjectValidationException("ApplyDraft");
            var profile = draft.Build();
            var proposed = Current();
            var index = proposed.Profiles.FindIndex(p => p.Id == profile.Id);
            if (index < 0) proposed.Profiles.Add(profile); else proposed.Profiles[index] = profile;
            _ = ProjectJson.Serialize(proposed);
            _updatingProfiles = true;
            try
            {
                if (index < 0) Profiles.Add(profile); else Profiles[index] = profile;
                if (_selected?.Id == profile.Id) { _selected = profile; SetEditor(profile); Changed(nameof(SelectedProfile)); }
            }
            finally { _updatingProfiles = false; }
            RefreshDeviceDirectory(); Dirty(); FileFeedback = Loc.Get("Ux.ProfileKept"); stored = true;
        });
        return stored;
    }

    public ProjectProfile? ProfileForApply(string id)
    {
        ProjectProfile? result = null;
        Run(() =>
        {
            if (_editorDirty && _editor.Id == id) throw new ProjectValidationException("ApplyDraft");
            // Validate and deep-copy retained intent; applying never saves a draft.
            result = ProjectJson.Parse(ProjectJson.Serialize(Current())).Profiles.SingleOrDefault(p => p.Id == id)
                ?? throw new ProjectValidationException("Reference");
        });
        return result;
    }

    private void Open()
    {
        var path = _dialogs.OpenPath();
        if (path == null) return;
        var snapshot = _store.Open(path); // Validate before replacing any current state.
        if (CanDiscard()) { Load(snapshot.Data, snapshot); FileFeedback = Loc.Get("Ux.Opened"); }
    }
    private void ChooseFolder()
    {
        var folder = _dialogs.ChooseFolder();
        if (folder == null) return;
        _folder = Path.GetDirectoryName(ProjectStore.ValidatePath(Path.Combine(folder, "project.avnet.json")));
        Changed(nameof(Folder));
    }
    private void Save(bool saveAs)
    {
        if (_editorDirty) KeepProfile();
        if (_deviceEditorDirty) KeepDevice();
        var data = Current();
        _ = ProjectJson.Serialize(data);
        var path = !saveAs && _snapshot != null ? _snapshot.Path : _dialogs.SavePath(_folder);
        if (path == null) return;
        path = ProjectStore.ValidatePath(path);
        var currentRevision = _store.RevisionAt(path);
        // For the opened file retain the revision originally opened, even after external edits.
        var expected = _snapshot != null && string.Equals(path, _snapshot.Path, StringComparison.OrdinalIgnoreCase)
            ? _snapshot.Revision : currentRevision;
        if (currentRevision != null && !_dialogs.ConfirmOverwrite(path)) return;
        var saved = _store.Save(data, path, expected);
        _snapshot = saved;
        _project = data;
        _documentDirty = false;
        _folder = Path.GetDirectoryName(saved.Path);
        Changed(nameof(FilePath)); Changed(nameof(Folder)); Changed(nameof(BackupPath)); Changed(nameof(HasUnsavedChanges));
        FileFeedback = Loc.Get("Ux.Saved");
    }
    private NetworkProject Current() => _project with { Profiles = Profiles.ToList(), Devices = Devices.ToList() };
    private void KeepProfile()
    {
        var profile = _editor.Build();
        var proposed = Current();
        var index = proposed.Profiles.FindIndex(p => p.Id == profile.Id);
        if (index >= 0) proposed.Profiles[index] = profile;
        else proposed.Profiles.Add(profile);
        _ = ProjectJson.Serialize(proposed);
        _updatingProfiles = true;
        try
        {
            if (index >= 0) Profiles[index] = profile;
            else Profiles.Add(profile);
        }
        finally { _updatingProfiles = false; }
        _selected = profile;
        SetEditor(profile);
        Changed(nameof(SelectedProfile));
        _editorDirty = false;
        RefreshDeviceDirectory();
        Dirty();
        FileFeedback = Loc.Get("Ux.ProfileKept");
    }
    private void DeleteProfile()
    {
        if (_selected == null) return;
        if (Devices.Any(d => d.ProfileId == _selected.Id) || _deviceEditor.ProfileId == _selected.Id)
            throw new ProjectValidationException("Reference");
        if (!_dialogs.ConfirmDelete()) return;
        _updatingProfiles = true;
        try { Profiles.Remove(_selected); }
        finally { _updatingProfiles = false; }
        _selected = null;
        SetEditor(new ProjectProfile());
        Changed(nameof(SelectedProfile));
        RefreshDeviceDirectory();
        Dirty();
    }
    private void Load(NetworkProject project, ProjectSnapshot? snapshot)
    {
        // Suppress selection callbacks while replacing the collection.
        _editorDirty = false;
        _selected = null;
        _project = project;
        _snapshot = snapshot;
        Profiles.Clear();
        foreach (var profile in project.Profiles) Profiles.Add(profile);
        LoadDevices(project.Devices);
        _folder = snapshot == null ? _folder : Path.GetDirectoryName(snapshot.Path);
        SetEditor(new ProjectProfile());
        _documentDirty = snapshot == null;
        Changed(string.Empty);
    }
    private void SetEditor(ProjectProfile profile)
    {
        _editor.PropertyChanged -= EditorChanged;
        _editor = new ProjectProfileEditor(profile);
        AttachEditor();
        _editorDirty = false;
        Changed(nameof(Editor)); Changed(nameof(HasUnsavedChanges));
    }
    private void AttachEditor() => _editor.PropertyChanged += EditorChanged;
    private void EditorChanged(object? sender, PropertyChangedEventArgs e)
    {
        FileFeedback = "";
        _editorDirty = true;
        Changed(nameof(HasUnsavedChanges));
    }
    private void Dirty() { FileFeedback = ""; _documentDirty = true; Changed(nameof(HasUnsavedChanges)); }
    private void Run(Action action)
    {
        Error = "";
        FileFeedback = "";
        try { action(); }
        catch (ProjectValidationException ex)
        {
            Error = Loc.Get("Project.Error" + ex.Code, Loc.Get("Project.Error", "Invalid project")) + " (" + ex.Field + ")";
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is NotSupportedException)
        {
            Error = Loc.Get("Project.ErrorIo", "Cannot access project file. Original/backup files have been retained.");
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed([CallerMemberName] string? property = null)
    {
        PropertyChanged?.Invoke(this, new(property));
        if (string.IsNullOrEmpty(property) || property == nameof(SelectedProfile) || property == nameof(SelectedDevice) || property == nameof(HasUnsavedChanges))
            CommandManager.InvalidateRequerySuggested();
    }
}

public sealed partial class ProjectProfileEditor : INotifyPropertyChanged
{
    private readonly string _id;
    private string _name, _mode, _alias, _address, _prefix, _gateway, _dnsMode, _dns1, _dns2;
    public ProjectProfileEditor() : this(new ProjectProfile()) { }
    public ProjectProfileEditor(ProjectProfile profile)
    {
        _id = profile.Id; _name = profile.Name; _mode = profile.Mode;
        _alias = profile.AdapterHint?.PreferredAlias ?? "";
        _address = profile.Ipv4?.Address ?? "";
        _prefix = (profile.Ipv4?.PrefixLength ?? 24).ToString(CultureInfo.InvariantCulture);
        _gateway = profile.Ipv4?.Gateway ?? "";
        _dnsMode = profile.Ipv4?.Dns.Mode ?? "leave-unchanged";
        _dns1 = profile.Ipv4?.Dns.Servers.ElementAtOrDefault(0) ?? "";
        _dns2 = profile.Ipv4?.Dns.Servers.ElementAtOrDefault(1) ?? "";
    }
    public string Id => _id;
    public string Name { get => _name; set => Set(ref _name, value); }
    public string Mode { get => _mode; set { if (_mode == value) return; Set(ref _mode, value); Changed(nameof(IsStatic)); } }
    public string Alias { get => _alias; set => Set(ref _alias, value); }
    public string Address { get => _address; set => Set(ref _address, value); }
    public string Prefix { get => _prefix; set => Set(ref _prefix, value); }
    public string Gateway { get => _gateway; set => Set(ref _gateway, value); }
    public string DnsMode { get => _dnsMode; set { if (_dnsMode == value) return; Set(ref _dnsMode, value); Changed(nameof(IsDnsStatic)); } }
    public string Dns1 { get => _dns1; set => Set(ref _dns1, value); }
    public string Dns2 { get => _dns2; set => Set(ref _dns2, value); }
    public bool IsStatic => Mode == "static";
    public bool IsDnsStatic => DnsMode == "static";
    public ProjectProfile Build()
    {
        if (IsStatic && !int.TryParse(Prefix, NumberStyles.None, CultureInfo.InvariantCulture, out _))
            throw new ProjectValidationException("Subnet", "prefixLength");
        if (IsStatic && IsDnsStatic && string.IsNullOrWhiteSpace(Dns1) && !string.IsNullOrWhiteSpace(Dns2))
            throw new ProjectValidationException("Structure", "dns.servers");
        return new ProjectProfile
        {
            Id = _id, Name = Name.Trim(), Mode = Mode,
            AdapterHint = string.IsNullOrWhiteSpace(Alias) ? null : new ProjectAdapterHint { PreferredAlias = Alias },
            Ipv4 = !IsStatic ? null : new ProjectIpv4
            {
                Address = Address.Trim(), PrefixLength = int.Parse(Prefix, CultureInfo.InvariantCulture),
                Gateway = string.IsNullOrWhiteSpace(Gateway) ? null : Gateway.Trim(),
                Dns = new ProjectDns
                {
                    Mode = DnsMode,
                    Servers = IsDnsStatic ? new[] { Dns1.Trim(), Dns2.Trim() }.Where(x => x.Length > 0).ToList() : new List<string>()
                }
            }
        };
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Set(ref string field, string value, [CallerMemberName] string? property = null)
    {
        if (field == value) return;
        field = value;
        Changed(property);
        if (_showValidation) Changed(string.Empty);
    }
    private void Changed([CallerMemberName] string? property = null) => PropertyChanged?.Invoke(this, new(property));
}
