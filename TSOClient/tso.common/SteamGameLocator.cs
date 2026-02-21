using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace FSO.Common
{
    /// <summary>
    /// Cross-platform Steam game locator. Finds game install paths and Proton save paths
    /// by reading Steam's libraryfolders.vdf across all configured library locations.
    /// </summary>
    public static class SteamGameLocator
    {
        public const int TS1LegacyAppId = 3314060; // The Sims: Legacy Collection

        private static readonly Regex LibraryPathRegex = new Regex("\"path\"\\s+\"(?<path>[^\"]+)\"", RegexOptions.Compiled);
        private static readonly Regex InstallDirRegex  = new Regex("\"installdir\"\\s+\"(?<dir>[^\"]+)\"",  RegexOptions.Compiled);

        /// <summary>
        /// Returns the install path of a Steam game by App ID, or null if not found.
        /// </summary>
        public static string GetGamePath(int appId)
        {
            foreach (var library in GetSteamLibraryPaths())
            {
                var manifest = Path.Combine(library, "steamapps", $"appmanifest_{appId}.acf");
                if (!File.Exists(manifest)) continue;

                if (TryGetInstallDir(manifest, out var installDir))
                {
                    var gamePath = Path.Combine(library, "steamapps", "common", installDir);
                    if (Directory.Exists(gamePath))
                        return gamePath + "/";
                }
            }
            return null;
        }

        /// <summary>
        /// On Linux, returns the path inside the Proton (Wine) prefix for a given game and sub-path.
        /// E.g. GetProtonSavePath(3314060, "Saved Games/Electronic Arts/The Sims 25")
        /// </summary>
        public static string GetProtonSavePath(int appId, string subPath)
        {
            foreach (var library in GetSteamLibraryPaths())
            {
                var candidate = Path.Combine(
                    library, "steamapps", "compatdata", appId.ToString(),
                    "pfx", "drive_c", "users", "steamuser", subPath);

                if (Directory.Exists(candidate))
                    return candidate + "/";
            }
            return null;
        }

        /// <summary>
        /// Enumerates all Steam library roots by reading libraryfolders.vdf from each known Steam location.
        /// </summary>
        public static IEnumerable<string> GetSteamLibraryPaths()
        {
            foreach (var steamRoot in GetSteamRoots())
            {
                if (!Directory.Exists(steamRoot)) continue;
                yield return steamRoot;

                var vdf = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
                if (!File.Exists(vdf)) continue;

                foreach (var extra in ParseLibraryFolders(vdf))
                    if (Directory.Exists(extra)) yield return extra;
            }
        }

        /// <summary>
        /// Returns platform-specific Steam root paths to search.
        /// On Windows, also checks the registry.
        /// </summary>
        private static IEnumerable<string> GetSteamRoots()
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            if (OperatingSystem.IsLinux())
            {
                yield return Path.Combine(home, ".local", "share", "Steam");
                yield return Path.Combine(home, ".steam", "steam");
                yield return Path.Combine(home, ".steam", "debian-installation");
            }
            else if (OperatingSystem.IsMacOS())
            {
                yield return Path.Combine(home, "Library", "Application Support", "Steam");
            }
            else if (OperatingSystem.IsWindows())
            {
                var regPath = GetWindowsSteamPath();
                if (regPath != null) yield return regPath;

                yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam");
                yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam");
            }
        }

        private static string GetWindowsSteamPath()
        {
            if (!OperatingSystem.IsWindows()) return null;
            try
            {
                return Microsoft.Win32.Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Valve\Steam", "InstallPath", null)?.ToString()
                    ?? Microsoft.Win32.Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath", null)?.ToString();
            }
            catch { return null; }
        }

        private static IEnumerable<string> ParseLibraryFolders(string vdfPath)
        {
            string[] lines;
            try { lines = File.ReadAllLines(vdfPath); }
            catch (IOException) { yield break; }

            foreach (var line in lines)
            {
                var m = LibraryPathRegex.Match(line);
                if (m.Success)
                    yield return m.Groups["path"].Value.Replace(@"\\", "/");
            }
        }

        private static bool TryGetInstallDir(string manifestPath, out string installDir)
        {
            try
            {
                foreach (var line in File.ReadLines(manifestPath))
                {
                    var m = InstallDirRegex.Match(line);
                    if (m.Success)
                    {
                        installDir = m.Groups["dir"].Value;
                        return true;
                    }
                }
            }
            catch (IOException) { }

            installDir = string.Empty;
            return false;
        }
    }
}
