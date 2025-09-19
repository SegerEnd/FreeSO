using CommandLine;
using CommandLine.Text;
using FSO.Server.Database;
using FSO.Server.DataService;
using FSO.Server.Utils;
using Ninject;

namespace FSO.Server
{
    public class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length == 0) args = new[] { "run" };

            int exitCode = Parser.Default.ParseArguments<
                RunServerOptions,
                DatabaseInitOptions,
                ImportNhoodOptions,
                RestoreLotsOptions,
                SqliteImportOptions,
                DataTrimOptions,
                ArchiveConvertOptions,
                ImportArchiveFeaturedOptions>(args)
                .MapResult(
                    (RunServerOptions opts) => RunTool<ToolRunServer>(opts),
                    (DatabaseInitOptions opts) => RunTool<ToolInitDatabase>(opts),
                    (ImportNhoodOptions opts) => RunTool<ToolImportNhood>(opts),
                    (RestoreLotsOptions opts) => RunTool<ToolRestoreLots>(opts),
                    (SqliteImportOptions opts) => RunTool<ToolSqliteImport>(opts),
                    (DataTrimOptions opts) => RunTool<ToolDataTrim>(opts),
                    (ArchiveConvertOptions opts) => RunTool<ToolArchiveConvert>(opts),
                    (ImportArchiveFeaturedOptions opts) => RunTool<ToolImportArchiveFeatured>(opts),
                    errs =>
                    {
                        // Here we explicitly pass the ParserResult type
                        var parserResult = errs as ParserResult<object> ?? null;
                        var helpText = HelpText.AutoBuild(
                            parserResult,
                            h =>
                            {
                                h.AdditionalNewLineAfterOption = false;
                                h.Heading = "FSO.Server Command Line Options";
                                return h;
                            },
                            e => e
                        );

                        Console.WriteLine(helpText);
                        return 1;
                    });

            return exitCode;
        }

        private static int RunTool<TTool>(object options) where TTool : ITool
        {
            var kernel = new StandardKernel(
                new ServerConfigurationModule(),
                new DatabaseModule(),
                new GlobalDataServiceModule(),
                new GluonHostPoolModule()
            );

            var tool = (ITool)kernel.Get(typeof(TTool), new Ninject.Parameters.ConstructorArgument("options", options));
            return tool.Run();
        }
    }
}
