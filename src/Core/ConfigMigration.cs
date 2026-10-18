using System;
using System.IO;
namespace FrontlineMap.Core
{
    public static class ConfigMigration
    {
        public static bool CopyIfMissing(string directory)
        {
            string legacy = Path.Combine(directory, "com.yuulf.frontlinemap.cfg"), current = Path.Combine(directory, "com.noms.frontlinemap.cfg");
            if (!File.Exists(legacy) || File.Exists(current))
                return false;
            string backup = legacy + ".backup-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            File.Copy(legacy, backup, false);
            File.Copy(legacy, current, false);
            return true;
        }
    }
}
