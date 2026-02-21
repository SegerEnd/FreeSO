using System;
using System.IO;
using FSO.Common;

namespace FSO.Client.Utils.GameLocator
{
    public class MacOSLocator : ILocator
    {
        public string FindTheSimsOnline()
        {
            string localDir = @"../The Sims Online/TSOClient/";
            if (File.Exists(Path.Combine(localDir, "tuning.dat"))) return localDir;

            return string.Format("{0}/The Sims Online/TSOClient/", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        }

        public string FindTheSims1()
        {
            // Check relative directory first (portable install)
            string localDir = @"../The Sims/";
            if (File.Exists(Path.Combine(localDir, "GameData", "Behavior.iff"))) return localDir;

            // Check Steam (Legacy Collection via CrossOver/Proton)
            var steamPath = SteamGameLocator.GetGamePath(SteamGameLocator.TS1LegacyAppId);
            if (steamPath != null) return steamPath;

            return null;
        }
    }
}
