using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Tocsoft.GraphQLCodeGen.Cli
{
    class Program
    {
        static CodeGeneratorSettingsLoader settingsLoader = new CodeGeneratorSettingsLoader();
        public static int Main(string[] args)
        {
            app = new CommandLineApplication();
            app.Name = "GraphQL Code Gen";
            app.HelpOption("-?|-h|--help");

            sourceArgument = app.Argument("source", "The settings file for gerating the code from", true);
            msbuildMode= app.Option("--msbuild-outputdir", "The directory", CommandOptionType.SingleValue);
            
            app.OnExecute((Func<Task<int>>)OnExecuteAsync);

            return app.Execute(args);
        }

        private static CommandArgument sourceArgument;
        private static CommandOption overrideOutput;
        private static CommandOption msbuildMode;
        private static CommandLineApplication app;

        public static async Task<int> OnExecuteAsync()
        {
            var settings = settingsLoader.LoadFromPath(sourceArgument.Value);
            foreach (var s in settings)
            {
                if (msbuildMode.HasValue())
                {
                    var fn = Path.GetFileName(s.OutputPath);
                    fn= $"{s.Namespace}.{s.ClassName}.{fn}";
                    s.OutputPath = Path.Combine(msbuildMode.Value(), fn);
                    Console.WriteLine(s.OutputPath);
                }

                var generator = new CodeGenerator(s);

                await generator.GenerateAsync();
            }

            return 0;
        }

    }
}
