using System.Collections.Generic;
using System.IO;
using System.Linq;
using FSO.Common;

namespace FSO.Client.Model.FamilyCity
{
    /// <summary>
    /// Lists/creates/opens Family Cities under <c>{UserDir}/FamilyCities/</c>.
    /// </summary>
    public static class FamilyCityManager
    {
        public static string RootDirectory => Path.Combine(FSOEnvironment.UserDir, "FamilyCities");

        public static IEnumerable<string> ListCityNames()
        {
            if (!Directory.Exists(RootDirectory)) return new string[0];
            return Directory.EnumerateDirectories(RootDirectory)
                .Select(d => new DirectoryInfo(d).Name)
                .OrderBy(n => n);
        }

        public static FamilyCity Open(string name)
        {
            var path = Path.Combine(RootDirectory, name);
            return new FamilyCity(name, path);
        }

        public static FamilyCity Create(string name)
        {
            var path = Path.Combine(RootDirectory, name);
            if (Directory.Exists(path))
                throw new IOException("A Family City named '" + name + "' already exists.");
            Directory.CreateDirectory(path);
            var city = new FamilyCity(name, path);
            city.Families.Save();
            return city;
        }

        public static bool Exists(string name)
        {
            return Directory.Exists(Path.Combine(RootDirectory, name));
        }
    }
}
