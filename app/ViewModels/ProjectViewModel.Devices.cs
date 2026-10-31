using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using NetworkProfileManager.Commands;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;

namespace NetworkProfileManager.ViewModels;

public sealed partial class ProjectViewModel
{
    private IBrowserLauncher _browser = null!;
    private ProjectDevice? _selectedDevice;
    private ProjectDeviceEditor _deviceEditor = new();
    private bool _deviceEditorDirty, _updatingDevices;
    private string _search = "", _groupBy = "type", _deviceNotice = "";
    private int _section;
    private ICollectionView _deviceView = new ListCollectionView(Array.Empty<DeviceDirectoryEntry>());

    public ObservableCollection<ProjectDevice> Devices { get; } = new();
    public ICollectionView DeviceView => _deviceView;
    public DeviceDirectoryEntry? SelectedDeviceEntry
    {
        get => _deviceView.Cast<DeviceDirectoryEntry>().FirstOrDefault(r => ReferenceEquals(r.Device, _selectedDevice));
        set
        {
            if (_updatingDevices) return;
            SelectedDevice = value?.Device;
            Changed();
        }
    }
    public bool NoDeviceMatches => _deviceView.IsEmpty;
    public ProjectDeviceEditor DeviceEditor => _deviceEditor;
    public IReadOnlyList<DeviceProfileChoice> DeviceProfiles => new[]
        { new DeviceProfileChoice("", Loc.Get("Device.Unlinked")) }
        .Concat(Profiles.Select(p => new DeviceProfileChoice(p.Id, p.Name))).ToList();
    public string DeviceProfileStatus => string.IsNullOrEmpty(_deviceEditor.ProfileId) ? Loc.Get("Device.Unlinked")
        : Profiles.FirstOrDefault(p => p.Id == _deviceEditor.ProfileId)?.Name ?? Loc.Get("Device.StaleProfile");
    public string DeviceDestination
    {
        get
        {
            try { ProjectJson.ValidateUrl(_deviceEditor.WebUrl); return _deviceEditor.WebUrl; }
            catch (ProjectValidationException) { return Loc.Get("Device.InvalidDestination"); }
        }
    }
    public string DeviceNotice { get => _deviceNotice; private set { _deviceNotice = value; Changed(); } }
    public int Section
    {
        get => _section;
        set { _section = value; Changed(); Changed(nameof(IsProfilesSection)); Changed(nameof(IsDevicesSection)); }
    }
    public bool IsProfilesSection => Section == 0;
    public bool IsDevicesSection => Section == 1;
    public string DeviceSearch
    {
        get => _search;
        set { if (_search == value) return; _search = value; Changed(); RefreshDeviceDirectory(); }
    }
    public string DeviceGroupBy
    {
        get => _groupBy;
        set { if (_groupBy == value) return; _groupBy = value; Changed(); RefreshDeviceDirectory(); }
    }
    public ProjectDevice? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (_updatingDevices || ReferenceEquals(value, _selectedDevice)) return;
            if (_deviceEditorDirty && !_dialogs.ConfirmDiscard()) { Changed(); Changed(nameof(SelectedDeviceEntry)); return; }
            _selectedDevice = value;
            SetDeviceEditor(value ?? new ProjectDevice());
            DeviceNotice = "";
            Changed();
            Changed(nameof(SelectedDeviceEntry));
        }
    }
    public ICommand NewDeviceCommand { get; private set; } = null!;
    public ICommand KeepDeviceCommand { get; private set; } = null!;
    public ICommand DeleteDeviceCommand { get; private set; } = null!;
    public ICommand OpenDeviceCommand { get; private set; } = null!;
    public ICommand UnlinkDeviceCommand { get; private set; } = null!;

    private void InitializeDevices(IBrowserLauncher browser)
    {
        _browser = browser;
        _deviceEditor.PropertyChanged += DeviceEditorChanged;
        NewDeviceCommand = new RelayCommand(_ => Run(() =>
        {
            if (_deviceEditorDirty && !_dialogs.ConfirmDiscard()) return;
            _selectedDevice = null;
            SetDeviceEditor(new ProjectDevice());
            DeviceNotice = "";
            Changed(nameof(SelectedDevice));
            Changed(nameof(SelectedDeviceEntry));
        }));
        KeepDeviceCommand = new RelayCommand(_ => Run(KeepDevice));
        DeleteDeviceCommand = new RelayCommand(_ => Run(DeleteDevice), _ => _selectedDevice != null);
        OpenDeviceCommand = new RelayCommand(_ => Run(OpenDevice), _ => _selectedDevice != null);
        UnlinkDeviceCommand = new RelayCommand(_ => _deviceEditor.ProfileId = "");
        RefreshDeviceDirectory();
    }
    private NetworkProject WithDevice(ProjectDevice device)
    {
        ProjectJson.ValidateUrl(device.WebUrl);
        var proposed = Current();
        var index = proposed.Devices.FindIndex(d => d.Id == device.Id);
        if (index < 0) proposed.Devices.Add(device); else proposed.Devices[index] = device;
        _ = ProjectJson.Serialize(proposed);
        return proposed;
    }
    private void KeepDevice()
    {
        var device = _deviceEditor.Build();
        _ = WithDevice(device); // Reject before changing the collection or draft.
        var index = Devices.ToList().FindIndex(d => d.Id == device.Id);
        _updatingDevices = true;
        try
        {
            if (index < 0) Devices.Add(device); else Devices[index] = device;
            _selectedDevice = device;
            SetDeviceEditor(device);
            RefreshDeviceDirectory();
        }
        finally { _updatingDevices = false; }
        Changed(nameof(SelectedDevice));
        DeviceNotice = "";
        Dirty();
    }
    private void DeleteDevice()
    {
        if (_selectedDevice == null || !_dialogs.ConfirmDeleteDevice()) return;
        _updatingDevices = true;
        try
        {
            Devices.Remove(_selectedDevice);
            _selectedDevice = null;
            SetDeviceEditor(new ProjectDevice());
            RefreshDeviceDirectory();
        }
        finally { _updatingDevices = false; }
        Changed(nameof(SelectedDevice));
        DeviceNotice = "";
        Dirty();
    }
    private void OpenDevice()
    {
        DeviceNotice = "";
        if (_selectedDevice == null) return;
        var device = _deviceEditor.Build();
        _ = WithDevice(device); // Includes schema, refs and shared URL policy, without retaining edits.
        var uri = new Uri(device.WebUrl, UriKind.Absolute);
        var mismatch = device.Ip != null && !string.Equals(device.Ip, uri.Host, StringComparison.OrdinalIgnoreCase);
        if (!_dialogs.ConfirmOpenDevice(device.WebUrl, uri.Scheme == "http", mismatch, device.ProfileId == null)) return;
        try { _browser.Open(device.WebUrl); }
        catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException || ex is IOException
            || ex is UnauthorizedAccessException || ex is ArgumentException || ex is NotSupportedException)
        {
            // Browser exceptions can contain the URL. Never display/log them.
            Error = Loc.Get("Device.LaunchError");
            return;
        }
        DeviceNotice = Loc.Get("Device.HandedToBrowser");
    }
    private void LoadDevices(IEnumerable<ProjectDevice> devices)
    {
        _updatingDevices = true;
        try
        {
            _selectedDevice = null;
            Devices.Clear();
            foreach (var device in devices) Devices.Add(device);
            SetDeviceEditor(new ProjectDevice());
            DeviceNotice = "";
            RefreshDeviceDirectory();
        }
        finally { _updatingDevices = false; }
    }
    private void RefreshDeviceDirectory()
    {
        // Rebuild only presentation rows, never the editor. Filtering/grouping
        // must not discard a draft, even when its row is temporarily hidden.
        var wasUpdating = _updatingDevices;
        _updatingDevices = true;
        try
        {
            var rows = Devices.Select(d =>
            {
                var profile = Profiles.FirstOrDefault(p => p.Id == d.ProfileId)?.Name ?? Loc.Get("Device.Unlinked");
                var group = DeviceGroupBy == "profile"
                    ? new DeviceDirectoryGroup(d.ProfileId ?? "", profile)
                    : new DeviceDirectoryGroup(d.Type ?? "", string.IsNullOrWhiteSpace(d.Type) ? Loc.Get("Device.OtherType") : d.Type);
                return new DeviceDirectoryEntry(d, group, profile);
            }).Where(r => new[] { r.Device.Name, r.Device.Type, r.Device.Manufacturer, r.Device.Ip,
                    r.Device.WebUrl, r.Device.Notes, r.ProfileName }
                .Any(s => s?.Contains(DeviceSearch, StringComparison.OrdinalIgnoreCase) == true))
                .OrderBy(r => r.Group.Name, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(r => r.Group.Id, StringComparer.Ordinal)
                .ThenBy(r => r.Device.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
            var view = new ListCollectionView(rows);
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(DeviceDirectoryEntry.Group)));
            _deviceView = view;
            Changed(nameof(DeviceView)); Changed(nameof(NoDeviceMatches));
            Changed(nameof(DeviceProfiles)); Changed(nameof(DeviceProfileStatus)); Changed(nameof(SelectedDevice));
            Changed(nameof(SelectedDeviceEntry));
        }
        finally { _updatingDevices = wasUpdating; }
    }
    private void SetDeviceEditor(ProjectDevice device)
    {
        _deviceEditor.PropertyChanged -= DeviceEditorChanged;
        _deviceEditor = new ProjectDeviceEditor(device);
        _deviceEditor.PropertyChanged += DeviceEditorChanged;
        _deviceEditorDirty = false;
        Changed(nameof(DeviceEditor)); Changed(nameof(DeviceProfileStatus)); Changed(nameof(DeviceDestination)); Changed(nameof(HasUnsavedChanges));
    }
    private void DeviceEditorChanged(object? sender, PropertyChangedEventArgs e)
    {
        _deviceEditorDirty = true;
        DeviceNotice = "";
        Changed(nameof(DeviceProfileStatus)); Changed(nameof(DeviceDestination)); Changed(nameof(HasUnsavedChanges));
    }
}

public sealed record DeviceDirectoryEntry(ProjectDevice Device, DeviceDirectoryGroup Group, string ProfileName);
public sealed record DeviceDirectoryGroup(string Id, string Name);
public sealed record DeviceProfileChoice(string Id, string Name);

public sealed class ProjectDeviceEditor : INotifyPropertyChanged
{
    private readonly string _id;
    private string _name, _webUrl, _type, _manufacturer, _ip, _profileId, _notes;
    public ProjectDeviceEditor() : this(new ProjectDevice()) { }
    public ProjectDeviceEditor(ProjectDevice device)
    {
        _id = device.Id; _name = device.Name; _webUrl = device.WebUrl;
        _type = device.Type ?? ""; _manufacturer = device.Manufacturer ?? "";
        _ip = device.Ip ?? ""; _profileId = device.ProfileId ?? ""; _notes = device.Notes ?? "";
    }
    public string Id => _id;
    public string Name { get => _name; set => Set(ref _name, value); }
    public string WebUrl { get => _webUrl; set => Set(ref _webUrl, value); }
    public string Type { get => _type; set => Set(ref _type, value); }
    public string Manufacturer { get => _manufacturer; set => Set(ref _manufacturer, value); }
    public string Ip { get => _ip; set => Set(ref _ip, value); }
    public string ProfileId { get => _profileId; set { if (value != null) Set(ref _profileId, value); } }
    public string Notes { get => _notes; set => Set(ref _notes, value); }
    public ProjectDevice Build() => new()
    {
        Id = _id, Name = Name.Trim(), WebUrl = WebUrl, Type = Optional(Type),
        Manufacturer = Optional(Manufacturer), Ip = Optional(Ip), ProfileId = Optional(ProfileId), Notes = Optional(Notes)
    };
    private static string? Optional(string value) => string.IsNullOrEmpty(value) ? null : value;
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Set(ref string field, string value, [CallerMemberName] string? property = null)
    {
        if (field == value) return;
        field = value;
        PropertyChanged?.Invoke(this, new(property));
    }
}
