using Microsoft.Extensions.CommandLineUtils;
using System;
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

            app.OnExecute((Func<int>)OnExecuteAsync);

            return app.Execute(args);
        }

        private static CommandArgument sourceArgument;
        private static CommandLineApplication app;

        public static int OnExecuteAsync()
        {
            var settings = settingsLoader.LoadFromPath(sourceArgument.Value);
            foreach (var s in settings)
            {
                var generator = new CodeGenerator(s);

                generator.Generate();
            }

            return 0;
        }

    }
}
