using CommandLine;
using CommandLine.Text;

namespace FSO.Server
{
    [Verb("run", HelpText = "Run the servers configured in config.json")]
    public class RunServerOptions
    {
        [Option('d', "debug", Default = false, HelpText = "Launches a network debug interface")]
        public bool Debug { get; set; }
    }

    [Verb("db-init", HelpText = "Initialize the database.")]
    public class DatabaseInitOptions
    {
    }

    [Verb("import-nhood", HelpText = "Import the neighborhood stored in the given JSON file to the specified shard.")]
    public class ImportNhoodOptions
    {
        [Value(0, MetaName = "ShardId", HelpText = "Shard ID")]
        public int ShardId { get; set; }

        [Value(1, MetaName = "JSON", HelpText = "Path to JSON file")]
        public string JSON { get; set; }
    }

    [Verb("restore-lots", HelpText = "Create lots in the database from FSOV saves in the specified folder.")]
    public class RestoreLotsOptions
    {
        [Value(0, MetaName = "ShardId", HelpText = "Shard ID")]
        public int ShardId { get; set; }

        [Value(1, MetaName = "RestoreFolder", HelpText = "Path to restore folder")]
        public string RestoreFolder { get; set; }

        [Option('l', "location", Default = 0u, HelpText = "Override location to place the property.")]
        public uint Location { get; set; }

        [Option('t', "owner", Default = 0u, HelpText = "Override avatar id to own the property.")]
        public uint Owner { get; set; }

        [Option('c', "category", Default = -1, HelpText = "Override property category.")]
        public int Category { get; set; }

        [Option('r', "report", Default = false, HelpText = "Report changes that would be made restoring the lot.")]
        public bool Report { get; set; }

        [Option('o', "objects", Default = false, HelpText = "Create new database entries for objects.")]
        public bool Objects { get; set; }

        [Option('s', "safe", Default = false, HelpText = "Do not return objects that have been placed.")]
        public bool Safe { get; set; }

        [Option('d', "donate", Default = false, HelpText = "Convert all objects to donated.")]
        public bool Donate { get; set; }
    }

    [Verb("sqlite-import", HelpText = "Imports a MariaDB export from a given directory into an sqlite database.")]
    public class SqliteImportOptions
    {
        [Value(0, MetaName = "ImportDir", HelpText = "Path to import directory")]
        public string ImportDir { get; set; }
    }

    [Verb("data-trim", HelpText = "Remove unimportant data, and optionally sensitive information.")]
    public class DataTrimOptions
    {
        [Option('a', "anon", Default = false, HelpText = "Strip any private information.")]
        public bool Anon { get; set; }
    }

    [Verb("archive-convert", HelpText = "Convert the database for use as an archive server.")]
    public class ArchiveConvertOptions
    {
    }

    [Verb("import-archive-featured", HelpText = "Import the featured lots in the given JSON file to the specified shard.")]
    public class ImportArchiveFeaturedOptions
    {
        [Value(0, MetaName = "ShardId", HelpText = "Shard ID")]
        public int ShardId { get; set; }

        [Value(1, MetaName = "JSON", HelpText = "Path to JSON file")]
        public string JSON { get; set; }
    }

    public class ProgramOptions
    {
        public string GetUsage<T>(ParserResult<T> result)
        {
            return HelpText.AutoBuild(result, h =>
            {
                h.AdditionalNewLineAfterOption = false;
                h.Heading = "FSO.Server Command Line Options";
                return HelpText.DefaultParsingErrorsHandler(result, h);
            }, e => e);
        }
    }
}
