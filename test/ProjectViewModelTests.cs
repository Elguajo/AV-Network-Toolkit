using System.IO;
using NetworkProfileManager.Helpers;
using NetworkProfileManager.Models;
using NetworkProfileManager.Services;
using NetworkProfileManager.ViewModels;

namespace NetworkProfileManager.Tests;

[Collection("StaticState")]
public class ProjectViewModelTests : IDisposable
{
    private readonly TestDataDirectoryFixture _legacy = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "AVNetVM_" + Guid.NewGuid().ToString("N"));
    public ProjectViewModelTests() => Directory.CreateDirectory(_root);
    public void Dispose() { Directory.Delete(_root, true); _legacy.Dispose(); }
    private string FilePath => Path.Combine(_root, "project.avnet.json");

    private sealed class Dialogs : IProjectDialogs
    {
        public string? Open, Save, Folder;
        public bool Discard = true, Overwrite = true, Delete = true;
        public int OverwritePrompts;
        public string? OpenPath() => Open;
        public string? ChooseFolder() => Folder;
        public string? SavePath(string? folder) => Save;
        public bool ConfirmDiscard() => Discard;
        public bool ConfirmDelete() => Delete;
        public bool ConfirmOverwrite(string path) { OverwritePrompts++; return Overwrite; }
    }

    private (ProjectViewModel Vm, Dialogs Dialog) OpenSample()
    {
        new ProjectStore().Save(ProjectJsonTests.Sample(), FilePath);
        var dialogs = new Dialogs { Open = FilePath };
        var vm = new ProjectViewModel(dialogs);
        vm.OpenCommand.Execute(null);
        Assert.Empty(vm.Error);
        return (vm, dialogs);
    }

    [Fact]
    public void OpenChooseFolderAndSaveDoNotTouchLegacyStore()
    {
        var legacyPath = Path.Combine(_legacy.Root, "profiles_adapter.json");
        File.WriteAllText(legacyPath, "legacy sentinel");
        var (vm, dialogs) = OpenSample();
        dialogs.Folder = _root;
        var count = Directory.GetFiles(_root).Length;
        vm.ChooseFolderCommand.Execute(null);
        Assert.Equal(_root, vm.Folder);
        Assert.Equal(count, Directory.GetFiles(_root).Length);
        dialogs.Save = Path.Combine(_root, "copy.avnet.json");
        vm.SaveAsCommand.Execute(null);
        Assert.Empty(vm.Error);
        Assert.Equal("legacy sentinel", File.ReadAllText(legacyPath));
        Assert.Single(Directory.GetFiles(_legacy.Root));
        Assert.Equal(0, dialogs.OverwritePrompts);
    }

    [Fact]
    public void EditingProfileKeepsIdAndDeviceReference()
    {
        var (vm, dialogs) = OpenSample();
        vm.SelectedProfile = vm.Profiles[0];
        vm.Editor.Name = "Renamed";
        vm.Editor.Gateway = ""; // Explicit no-gateway intent in project editor.
        vm.Editor.DnsMode = "leave-unchanged";
        vm.KeepProfileCommand.Execute(null);
        Assert.Empty(vm.Error);
        dialogs.Save = FilePath;
        vm.SaveAsCommand.Execute(null);
        var result = new ProjectStore().Open(FilePath).Data;
        Assert.Equal("static", result.Profiles[0].Id);
        Assert.Equal("static", vm.Editor.Id);
        Assert.Equal("static", result.Devices[0].ProfileId);
        Assert.Equal("Renamed", result.Profiles[0].Name);
        Assert.Null(result.Profiles[0].Ipv4!.Gateway);
        Assert.Equal("leave-unchanged", result.Profiles[0].Ipv4!.Dns.Mode);
        Assert.Equal(1, dialogs.OverwritePrompts);
        Assert.False(vm.HasUnsavedChanges);
    }

    [Fact]
    public void CorruptOpenAndDeclinedDiscardPreserveCurrentProject()
    {
        var (vm, dialogs) = OpenSample();
        vm.ProjectName = "Draft";
        dialogs.Open = Path.Combine(_root, "broken.json"); File.WriteAllText(dialogs.Open, "broken");
        vm.OpenCommand.Execute(null);
        Assert.NotEmpty(vm.Error);
        Assert.Equal("Draft", vm.ProjectName);
        Assert.Equal(2, vm.Profiles.Count);
        dialogs.Open = FilePath; dialogs.Discard = false;
        vm.OpenCommand.Execute(null);
        Assert.Equal("Draft", vm.ProjectName);
        vm.NewCommand.Execute(null);
        Assert.Equal("Draft", vm.ProjectName);
    }

    [Fact]
    public void DeclinedOverwriteAndExternalEditPreserveTarget()
    {
        var (vm, dialogs) = OpenSample();
        var original = File.ReadAllBytes(FilePath);
        vm.ProjectName = "Changed"; dialogs.Save = FilePath; dialogs.Overwrite = false;
        vm.SaveAsCommand.Execute(null);
        Assert.Equal(original, File.ReadAllBytes(FilePath));
        Assert.True(vm.HasUnsavedChanges);
        dialogs.Overwrite = true; File.WriteAllText(FilePath, "external");
        vm.SaveAsCommand.Execute(null);
        Assert.NotEmpty(vm.Error);
        Assert.Equal("external", File.ReadAllText(FilePath));
        Assert.True(vm.HasUnsavedChanges);
    }

    [Fact]
    public void ReferencedProfileCannotBeDeleted()
    {
        var (vm, _) = OpenSample();
        vm.SelectedProfile = vm.Profiles[0];
        vm.DeleteProfileCommand.Execute(null);
        Assert.NotEmpty(vm.Error);
        Assert.Equal(2, vm.Profiles.Count);
    }

    [Fact]
    public void UnreferencedProfileDeletionRequiresConfirmation()
    {
        var (vm, dialogs) = OpenSample(); vm.SelectedProfile = vm.Profiles[1];
        dialogs.Delete = false; vm.DeleteProfileCommand.Execute(null);
        Assert.Equal(2, vm.Profiles.Count);
        dialogs.Delete = true; vm.DeleteProfileCommand.Execute(null);
        Assert.Single(vm.Profiles);
        Assert.True(vm.HasUnsavedChanges);
    }

    [Fact]
    public void PendingValidEditorIsIncludedInSaveAs()
    {
        var dialogs = new Dialogs { Save = FilePath };
        var vm = new ProjectViewModel(dialogs);
        var id = vm.Editor.Id;
        vm.Editor.Name = "DHCP profile";
        vm.SaveAsCommand.Execute(null);
        Assert.Empty(vm.Error);
        Assert.Equal(id, new ProjectStore().Open(FilePath).Data.Profiles[0].Id);
    }

    [Fact]
    public void InvalidPendingEditorCannotSaveOrLosePriorProfile()
    {
        var (vm, dialogs) = OpenSample(); vm.SelectedProfile = vm.Profiles[0];
        vm.Editor.Address = "invalid";
        dialogs.Save = Path.Combine(_root, "invalid.avnet.json");
        vm.SaveAsCommand.Execute(null);
        Assert.NotEmpty(vm.Error);
        Assert.False(File.Exists(dialogs.Save));
        Assert.Equal("192.0.2.10", vm.Profiles[0].Ipv4!.Address);
    }

    [Fact]
    public void SaveFailureLeavesDraftDirtyAndPathUnchanged()
    {
        var dialogs = new Dialogs { Save = FilePath };
        var vm = new ProjectViewModel(dialogs, new ProjectStore(_ => throw new IOException()));
        vm.ProjectName = "Draft";
        var previous = vm.FilePath;
        vm.SaveAsCommand.Execute(null);
        Assert.NotEmpty(vm.Error);
        Assert.Equal(previous, vm.FilePath);
        Assert.True(vm.HasUnsavedChanges);
        Assert.Empty(Directory.GetFiles(_root));
    }

    [Fact]
    public void EditorWithSecondaryOnlyDnsIsRejected()
    {
        var editor = new ProjectProfileEditor { Name = "Static", Mode = "static", Address = "192.0.2.10",
            DnsMode = "static", Dns2 = "192.0.2.53" };
        Assert.Throws<ProjectValidationException>(() => editor.Build());
    }
}
