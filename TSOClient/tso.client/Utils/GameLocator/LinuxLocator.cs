using System;
using System.IO;
using FSO.Common;

namespace FSO.Client.Utils.GameLocator
{
    public class LinuxLocator : ILocator
    {
        public string FindTheSimsOnline()
        {
            string localDir = @"../The Sims Online/TSOClient/";
            if (File.Exists(Path.Combine(localDir, "tuning.dat"))) return localDir;

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string homeDir = Path.Combine(home, "Documents", "The Sims Online", "TSOClient") + "/";
            if (File.Exists(Path.Combine(homeDir, "tuning.dat"))) return homeDir;

            return "game/TSOClient/";
        }

        public string FindTheSims1()
        {
            // Check relative directory first (portable install)
            string localDir = @"../The Sims/";
            if (File.Exists(Path.Combine(localDir, "GameData", "Behavior.iff"))) return localDir;

            // Check Steam (Legacy Collection via Proton)
            var steamPath = SteamGameLocator.GetGamePath(SteamGameLocator.TS1LegacyAppId);
            if (steamPath != null) return steamPath;

            return null;
        }
    }
}
