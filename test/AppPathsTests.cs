using System;
using System.IO;
using NetworkProfileManager.Helpers;

namespace NetworkProfileManager.Tests;

[Collection("StaticState")]
public class AppPathsTests
{
    [Fact]
    public void DataDirectory_RequiresBootstrapInsteadOfCreatingPersonalData()
    {
        AppPaths.ResetForTests();
        try
        {
            Assert.Throws<InvalidOperationException>(() => AppPaths.DataDirectory);
        }
        finally { AppPaths.OverrideDataDirectory(null); }
    }

    [Fact]
    public void OverrideDataDirectory_TakesEffect_AndCreatesFolder()
    {
        var temp = Path.Combine(Path.GetTempPath(),
            $"NPM_Override_{Guid.NewGuid():N}");
        try
        {
            AppPaths.OverrideDataDirectory(temp);
            var dir = AppPaths.DataDirectory;
            Assert.Equal(temp, dir);
            Assert.True(Directory.Exists(temp));
        }
        finally
        {
            AppPaths.OverrideDataDirectory(null);
            try { Directory.Delete(temp, recursive: true); } catch { }
        }
    }

    [Fact]
    public void FolderResolutionUsesInjectedUserProfileWithoutFilesystemAccess()
    {
        var paths = NetworkProfileManager.Services.UserStorageLocations.ForUser(@"C:\FixtureUser", @"D:\FixtureLocal", @"E:\FixtureExe");
        Assert.Equal(@"C:\FixtureUser\.av-network-toolkit", paths.Home);
        Assert.Equal(@"D:\FixtureLocal\NetworkProfileManager", paths.Legacy);
        Assert.Throws<IOException>(() => NetworkProfileManager.Services.UserStorageLocations.ForUser("", @"D:\FixtureLocal", @"E:\FixtureExe"));
    }
}
