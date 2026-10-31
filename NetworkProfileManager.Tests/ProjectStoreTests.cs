using System.Text;
using System.IO;
using NetworkProfileManager.Services;

namespace NetworkProfileManager.Tests;

public class ProjectStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "AVNetProject_" + Guid.NewGuid().ToString("N"));
    public ProjectStoreTests() => Directory.CreateDirectory(_root);
    public void Dispose() => Directory.Delete(_root, true);
    private string FilePath => Path.Combine(_root, "project.avnet.json");

    [Fact]
    public void OpenDoesNotWriteAnythingAndSaveAsPreservesIds()
    {
        var store = new ProjectStore();
        var saved = store.Save(ProjectJsonTests.Sample(), FilePath);
        var files = Directory.GetFiles(_root);
        var bytes = File.ReadAllBytes(FilePath);
        var opened = store.Open(FilePath);
        Assert.Equal(files, Directory.GetFiles(_root));
        Assert.Equal(bytes, File.ReadAllBytes(FilePath));
        var copy = store.Save(opened.Data, Path.Combine(_root, "copy.avnet.json"));
        Assert.Equal(saved.Data.Project.Id, copy.Data.Project.Id);
        Assert.Equal(saved.Revision, copy.Revision);
    }

    [Fact]
    public void ExistingTargetRequiresRevisionAndBackupRetainsExactOldBytes()
    {
        var store = new ProjectStore();
        var saved = store.Save(ProjectJsonTests.Sample(), FilePath);
        var bytes = File.ReadAllBytes(FilePath);
        Assert.Equal("Conflict", Assert.Throws<ProjectValidationException>(() => store.Save(saved.Data, FilePath)).Code);
        var updated = store.Save(saved.Data with { Project = saved.Data.Project with { Name = "Changed" } }, FilePath, saved.Revision);
        Assert.NotEqual(saved.Revision, updated.Revision);
        Assert.Equal(bytes, File.ReadAllBytes(updated.BackupPath!));
        Assert.Equal("Example", store.Open(updated.BackupPath!).Data.Project.Name);
        Assert.Single(Directory.GetFiles(_root, "*.bak"));
    }

    [Theory]
    [InlineData("Serialize")]
    [InlineData("TempWrite")]
    [InlineData("Flush")]
    [InlineData("Backup")]
    [InlineData("BackupFlush")]
    [InlineData("Replace")]
    [InlineData("AtomicReplace")]
    public void StageFailurePreservesOriginal(string stage)
    {
        var saved = new ProjectStore().Save(ProjectJsonTests.Sample(), FilePath);
        var original = File.ReadAllBytes(FilePath);
        var failing = new ProjectStore(current => { if (current.ToString() == stage) throw new IOException("simulated failure"); });
        Assert.Throws<IOException>(() => failing.Save(saved.Data, FilePath, saved.Revision));
        Assert.Equal(original, File.ReadAllBytes(FilePath));
        Assert.Empty(Directory.GetFiles(_root, "*.tmp"));
        Assert.Empty(Directory.GetFiles(_root, "*.lock"));
    }

    [Fact]
    public void ExternalEditIsNotOverwritten()
    {
        var store = new ProjectStore();
        var saved = store.Save(ProjectJsonTests.Sample(), FilePath);
        File.WriteAllText(FilePath, "external version");
        Assert.Equal("Conflict", Assert.Throws<ProjectValidationException>(() => store.Save(saved.Data, FilePath, saved.Revision)).Code);
        Assert.Equal("external version", File.ReadAllText(FilePath));
    }

    [Fact]
    public void LateExternalEditIsDetectedAfterBackup()
    {
        var saved = new ProjectStore().Save(ProjectJsonTests.Sample(), FilePath);
        var store = new ProjectStore(stage => { if (stage == ProjectWriteStage.Replace) File.WriteAllText(FilePath, "external version"); });
        Assert.Throws<ProjectValidationException>(() => store.Save(saved.Data, FilePath, saved.Revision));
        Assert.Equal("external version", File.ReadAllText(FilePath));
        Assert.NotEmpty(Directory.GetFiles(_root, "*.bak"));
    }

    [Fact]
    public void LockedTargetAndSaveLeaseFailWithoutChangingOriginal()
    {
        var store = new ProjectStore(); var saved = store.Save(ProjectJsonTests.Sample(), FilePath);
        var bytes = File.ReadAllBytes(FilePath);
        using (new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.None))
            Assert.Throws<IOException>(() => store.Save(saved.Data, FilePath, saved.Revision));
        using (new FileStream(FilePath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            Assert.Throws<IOException>(() => store.Save(saved.Data, FilePath, saved.Revision));
        Assert.Equal(bytes, File.ReadAllBytes(FilePath));
    }

    [Fact]
    public void ReplaceFailureRetainsOriginalAndCompleteBackup()
    {
        var saved = new ProjectStore().Save(ProjectJsonTests.Sample(), FilePath);
        var original = File.ReadAllBytes(FilePath);
        FileStream? locked = null;
        var store = new ProjectStore(stage =>
        {
            if (stage == ProjectWriteStage.AtomicReplace)
                locked = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        });
        try { Assert.Throws<IOException>(() => store.Save(saved.Data, FilePath, saved.Revision)); }
        finally { locked?.Dispose(); }
        Assert.Equal(original, File.ReadAllBytes(FilePath));
        Assert.Equal(original, File.ReadAllBytes(Assert.Single(Directory.GetFiles(_root, "*.bak"))));
    }

    [Fact]
    public void NonCooperatingReplacementRaceRetainsBothVersionsAndSignalsConflict()
    {
        var saved = new ProjectStore().Save(ProjectJsonTests.Sample(), FilePath);
        var original = File.ReadAllBytes(FilePath);
        var store = new ProjectStore(stage => { if (stage == ProjectWriteStage.AtomicReplace) File.WriteAllText(FilePath, "external racing edit"); });
        Assert.Equal("ConcurrentReplace", Assert.Throws<ProjectValidationException>(() => store.Save(saved.Data, FilePath, saved.Revision)).Code);
        var backups = Directory.GetFiles(_root, "*.bak");
        Assert.Equal(2, backups.Length);
        Assert.Contains(backups, p => File.ReadAllText(p) == "external racing edit");
        Assert.Contains(backups, p => File.ReadAllBytes(p).SequenceEqual(original));
        Assert.Equal(original, File.ReadAllBytes(FilePath));
    }

    [Theory]
    [InlineData("project.avnet.json")]
    [InlineData("https://192.0.2.1/project.json")]
    [InlineData(@"\\192.0.2.1\share\project.json")]
    [InlineData(@"C:\temp\project.json:stream")]
    [InlineData(@"\\?\C:\temp\project.json")]
    [InlineData(@"C:\temp\CON.json")]
    [InlineData(@"C:\temp\NUL.avnet.json")]
    [InlineData(@"C:\temp\COM1\project.json")]
    [InlineData(@"C:\temp\project.json.")]
    public void UnsafePathsAreRejectedBeforeAccess(string path)
        => Assert.Equal("Path", Assert.Throws<ProjectValidationException>(() => ProjectStore.ValidatePath(path)).Code);

    [Fact]
    public void CorruptUtf8AndJsonRemainUntouched()
    {
        var store = new ProjectStore();
        foreach (var bytes in new[] { new byte[] { 0xff, 0xfe, 0xff }, Encoding.UTF8.GetBytes("{broken") })
        {
            File.WriteAllBytes(FilePath, bytes);
            Assert.Throws<ProjectValidationException>(() => store.Open(FilePath));
            Assert.Equal(bytes, File.ReadAllBytes(FilePath));
            Assert.Single(Directory.GetFiles(_root));
        }
    }

    [Fact]
    public void InvalidOutputCannotCreateAFile()
    {
        Assert.Throws<ProjectValidationException>(() => new ProjectStore().Save(new(), FilePath));
        Assert.Empty(Directory.GetFiles(_root));
    }

    [Fact]
    public void ReadOnlyTargetPreservesOriginalOnPermissionFailure()
    {
        var store = new ProjectStore(); var saved = store.Save(ProjectJsonTests.Sample(), FilePath);
        var original = File.ReadAllBytes(FilePath);
        File.SetAttributes(FilePath, FileAttributes.ReadOnly);
        try
        {
            Assert.Throws<UnauthorizedAccessException>(() => store.Save(saved.Data, FilePath, saved.Revision));
            Assert.Equal(original, File.ReadAllBytes(FilePath));
        }
        finally { File.SetAttributes(FilePath, FileAttributes.Normal); }
    }
}
