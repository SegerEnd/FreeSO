using CommandLine;
using FSO.Common.DependencyInjection;
using FSO.Server.Database;
using FSO.Server.DataService;
using FSO.Server.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace FSO.Server
{
    public class Program
    {
        private readonly struct ToolInfo(Type toolType, object toolOptions)
        {
            public readonly Type ToolType = toolType;
            public readonly object ToolOptions = toolOptions;
        }

        public static int Main(string[] args)
        {
            ToolInfo? toolInfo = null;

            string[] a2 = args;
            if (args.Length == 0) a2 = new string[] { "run" };

            var options = new ProgramOptions();
            int result = Parser.Default.ParseArguments<
                RunServerOptions, DatabaseInitOptions, ImportNhoodOptions, RestoreLotsOptions,
                SqliteImportOptions, DataTrimOptions, ArchiveConvertOptions, ImportArchiveFeaturedOptions,
                PluginAnonymizeOptions>(a2)
                .MapResult(
                (RunServerOptions opts) =>
                {
                    toolInfo = new(typeof(ToolRunServer), opts);
                    return 0;
                },
                (DatabaseInitOptions opts) =>
                {
                    toolInfo = new(typeof(ToolInitDatabase), opts);
                    return 0;
                },
                (ImportNhoodOptions opts) =>
                {
                    toolInfo = new(typeof(ToolImportNhood), opts);
                    return 0;
                },
                (RestoreLotsOptions opts) =>
                {
                    toolInfo = new(typeof(ToolRestoreLots), opts);
                    return 0;
                },
                (SqliteImportOptions opts) =>
                {
                    toolInfo = new(typeof(ToolSqliteImport), opts);
                    return 0;
                },
                (DataTrimOptions opts) =>
                {
                    toolInfo = new(typeof(ToolDataTrim), opts);
                    return 0;
                },
                (ArchiveConvertOptions opts) =>
                {
                    toolInfo = new(typeof(ToolArchiveConvert), opts);
                    return 0;
                },
                (ImportArchiveFeaturedOptions opts) =>
                {
                    toolInfo = new(typeof(ToolImportArchiveFeatured), opts);
                    return 0;
                },
                (PluginAnonymizeOptions opts) =>
                {
                    toolInfo = new(typeof(ToolPluginAnonymize), opts);
                    return 0;
                },
                errs => 1
                );

            if (result == 1 || toolInfo == null)
            {
                Environment.Exit(1);
            }

            var container = ServiceContainer.Build(services =>
            {
                services.AddServerConfiguration();
                services.AddDatabaseServices();
                services.AddGlobalDataServices();
                services.AddGluonHostPool();
            });

            //If db init, allow @ variables in the query itself. We could always enable this but for added security
            //we are conditionally adding it only for db migrations
            if (toolInfo.Value.ToolType == typeof(ToolInitDatabase))
            {
                var config = container.Provider.Get<ServerConfiguration>();
                if (!config.Database.ConnectionString.EndsWith(";")){
                    config.Database.ConnectionString += ";";
                }
                config.Database.ConnectionString += "Allow User Variables=True";
            }

            var tool = (ITool)ActivatorUtilities.CreateInstance(container.Provider, toolInfo.Value.ToolType, toolInfo.Value.ToolOptions);
            return tool.Run();

        }
    }
}
