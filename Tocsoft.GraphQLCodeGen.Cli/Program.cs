using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Tocsoft.GraphQLCodeGen.Cli
{
    class Program
    {
        public static int Main(string[] args)
        {
            CommandLineApplication app = new CommandLineApplication();
            app.Name = "GraphQL Code Gen";
            app.HelpOption("-?|-h|--help");

            MainApplication(app);

#if DLL_INTROSPECTION
            app.Command("introspect", DllIntrospectionExecutor.Introspect);
#endif


            int result = app.Execute(args);

            //if (Debugger.IsAttached)
            //{
            //    Console.WriteLine();
            //    Console.WriteLine("------------ hit enter to close ---------------");
            //    Console.ReadLine();
            //}
            return result;
        }


        public static void MainApplication(CommandLineApplication app)
        {

            CommandArgument sourceArgument = app.Argument("source", "The query files for gerating the code from", true);
            CommandOption msbuildMode = app.Option("--msbuild-outputdir", "The directory", CommandOptionType.SingleValue);
            CommandOption format = app.Option("--format", "The export format", CommandOptionType.SingleValue);
            CommandOption overrideSettingsPath = app.Option("--settings", "The path to a settings file to override values", CommandOptionType.SingleValue);
            CommandOption defaultSettingsPath = app.Option("--default-settings", "The path to a settings file to load in first before walking the tree", CommandOptionType.SingleValue);

            app.OnExecute(async () =>
            {
                // based on the list of *.gql/*.graphql files we extract settings 
                // from comment headers and based on that we generate blocks of settings
                // the same query each setting value can be repeated and that will cause 
                // the collection to be duplicated too.
                var inMsbuildMode = msbuildMode.HasValue();

                var consoleLoger = new ConsoleLogger(inMsbuildMode);
                CodeGeneratorSettingsLoader settingsLoader = new CodeGeneratorSettingsLoader(consoleLoger);

                var loaderSettings = new CodeGeneratorSettingsLoaderDefaults();
                loaderSettings.Format = format.HasValue() ? format.Value() : "cs";
                if (inMsbuildMode)
                {
                    var targetPattern = Path.Combine(msbuildMode.Value(), "{classname}.cs");

                    var oldFF = loaderSettings.FixFile;
                    loaderSettings.FixFile = (f) =>
                    {
                        oldFF?.Invoke(f);

                        if (f.Format == "cs")
                        {
                            f.OutputPath = targetPattern;
                        }
                    };
                }

                if (defaultSettingsPath.HasValue())
                {
                    loaderSettings.DefaultPath = Path.GetFullPath(defaultSettingsPath.Value());
                }
                if (overrideSettingsPath.HasValue())
                {
                    loaderSettings.OverridesPath = Path.GetFullPath(overrideSettingsPath.Value());
                }

                IEnumerable<CodeGeneratorSettings> settings = settingsLoader.GenerateSettings(loaderSettings, sourceArgument.Values);
                HashSet<string> generatedFiles = new HashSet<string>();
                HashSet<string> failedFiles = new HashSet<string>();
                var semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
                var tasks = settings.Select(Generate).ToList();

                Task Generate(CodeGeneratorSettings s)
                {
                    return Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            CodeGenerator generator = new CodeGenerator(consoleLoger, s);
                            if (await generator.GenerateAsync())
                            {
                                // generated code in here
                                generatedFiles.Add(s.OutputPath);
                            }
                            else
                            {
                                failedFiles.Add(s.OutputPath);
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });
                }

                await Task.WhenAll(tasks);

                if (inMsbuildMode)
                {
                    foreach (string result in generatedFiles)
                    {
                        Console.WriteLine(result);
                    }
                }

                if (failedFiles.Any())
                {
                    return -1;
                }

                return 0;
            });
        }
    }
}
