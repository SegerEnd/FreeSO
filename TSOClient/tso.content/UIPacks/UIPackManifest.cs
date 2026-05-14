using System;
using System.IO;

namespace FSO.Content.UIPacks
{
    public class UIPackManifest
    {
        public string Id;
        public string Name;
        public string Author;
        public string Version;
        public string Description;

        public string SourcePath;
        public bool IsArchive;

        public static UIPackManifest Parse(Stream iniStream, string sourcePath, bool isArchive)
        {
            var m = new UIPackManifest { SourcePath = sourcePath, IsArchive = isArchive };
            using (var reader = new StreamReader(iniStream))
            {
                string section = null;
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                    if (line[0] == '[' && line[line.Length - 1] == ']')
                    {
                        section = line.Substring(1, line.Length - 2).Trim().ToLowerInvariant();
                        continue;
                    }
                    var eq = line.IndexOf('=');
                    if (eq < 0) continue;
                    var key = line.Substring(0, eq).Trim().ToLowerInvariant();
                    var value = line.Substring(eq + 1).Trim();
                    if (section == "pack") m.ApplyPackField(key, value);
                }
            }
            if (string.IsNullOrWhiteSpace(m.Id))
                m.Id = Path.GetFileNameWithoutExtension(sourcePath ?? "unnamed");
            if (string.IsNullOrWhiteSpace(m.Name)) m.Name = m.Id;
            return m;
        }

        private void ApplyPackField(string key, string value)
        {
            switch (key)
            {
                case "id": Id = value; break;
                case "name": Name = value; break;
                case "author": Author = value; break;
                case "version": Version = value; break;
                case "description": Description = value; break;
            }
        }
    }
}
