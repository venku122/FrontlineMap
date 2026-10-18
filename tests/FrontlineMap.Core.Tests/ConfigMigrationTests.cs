using Xunit;
using FrontlineMap.Core;
public class ConfigMigrationTests
{
    [Fact]
    public void MigrationBacksUpOldAndNeverOverwritesCurrent()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(path);
        try
        {
            var old = Path.Combine(path, "com.yuulf.frontlinemap.cfg");
            var current = Path.Combine(path, "com.noms.frontlinemap.cfg");
            File.WriteAllText(old, "old");
            Assert.True(ConfigMigration.CopyIfMissing(path));
            Assert.Equal("old", File.ReadAllText(current));
            Assert.Single(Directory.GetFiles(path, "*.backup-*"));
            File.WriteAllText(current, "new");
            Assert.False(ConfigMigration.CopyIfMissing(path));
            Assert.Equal("new", File.ReadAllText(current));
            Assert.Equal("old", File.ReadAllText(old));
        }
        finally { Directory.Delete(path, true); }
    }
}
