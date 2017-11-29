using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Tocsoft.GraphQLCodeGen.RelectionHelpers;

namespace Tocsoft.GraphQLCodeGen.Cli
{
    class Program
    {
        static CodeGeneratorSettingsLoader settingsLoader = new CodeGeneratorSettingsLoader();
        public static int Main(string[] args)
        {
            CommandLineApplication app = new CommandLineApplication();
            app.Name = "GraphQL Code Gen";
            app.HelpOption("-?|-h|--help");

            MainApplication(app);


            app.Command("introspect", Introspect);


            int result = app.Execute(args);

            if (Debugger.IsAttached)
            {
                Console.WriteLine();
                Console.WriteLine("------------ hit enter to close ---------------");
                Console.ReadLine();
            }
            return result;
        }


        public static void MainApplication(CommandLineApplication app)
        {

            CommandArgument sourceArgument = app.Argument("source", "The settings file for gerating the code from", true);
            CommandOption msbuildMode = app.Option("--msbuild-outputdir", "The directory", CommandOptionType.SingleValue);

            app.OnExecute(async () =>
            {
                // based on the list of *.gql/*.graphql files we extract settings 
                // from comment headers and based on that we generate blocks of settings
                // the same query each setting value can be repeated and that will cause 
                // the collection to be duplicated too.

                IEnumerable<CodeGeneratorSettings> settings = settingsLoader.GenerateSettings(sourceArgument.Values);
                List<string> generatedFiles = new List<string>();
                foreach (CodeGeneratorSettings s in settings)
                {
                    if (msbuildMode.HasValue())
                    {
                        string fn = Path.GetFileName(s.OutputPath);
                        fn = $"{s.Namespace}.{s.ClassName}.{fn}";
                        // only redirect c# files to the temp directory otherwise output normally
                        // maybe rework if/when er support other project types
                        if (fn.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                        {
                            s.OutputPath = Path.Combine(msbuildMode.Value(), fn);
                        }
                    }

                    CodeGenerator generator = new CodeGenerator(s);

                    await generator.GenerateAsync();
                }
                if (msbuildMode.HasValue())
                {
                    foreach (string result in settings.Select(x => x.OutputPath).Distinct())
                    {
                        Console.WriteLine(result);
                    }
                }
                return 0;
            });
        }

        public static void Introspect(CommandLineApplication app)
        {
            CommandArgument location = app.Argument("location", "The source of the introspection, be it a http endpoint or a dll or event a json file", true);

            CommandOption query = app.Option("--query <queryType>", "a pattern to discover a query type, can be eather a type or a pointer to types deccerated with [AttributeName]", CommandOptionType.MultipleValue);
            CommandOption mutation = app.Option("--mutation <mutationType>", "a mutation to discover a query type, can be eather a type or a pointer to types deccerated with [AttributeName]", CommandOptionType.MultipleValue);
            CommandOption output = app.Option("--output <path>", "the path to output to, if no provided it will output to console ", CommandOptionType.SingleValue);


            app.OnExecute(() =>
            {

#if NET461
                AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
                {
                    var name = args.Name.Split(new[] { ',' }, 2)[0];
                    var res =  AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(x =>
                    {
                        return x.GetName().Name == name;
                    });

                    return res;
                };
#else
                System.Runtime.Loader.AssemblyLoadContext.Default.Resolving += (a, b) =>
                {
                    return null;
                };
#endif
                // these are the ones we scan for the correct types, 
                IEnumerable<string> dlls = GlobExpander.FindFiles(location.Values);
                //all dlls in directoris next to any of the source dlls
                IEnumerable<string> directoreistoLoad = dlls.Select(x => Path.GetDirectoryName(x)).Distinct();

                // these are all the ones we need to load into the appdomain
                List<string> finalListToLoad = directoreistoLoad.SelectMany(x => Directory.EnumerateFiles(x, "*.dll")).Union(directoreistoLoad.SelectMany(x => Directory.EnumerateFiles(x, "*.exe"))).ToList();
                List<Assembly> searchableAssemblies = new List<Assembly>();
                List<Assembly> allAssemblies = new List<Assembly>();
                List<string> additionalAssembliesToLoad = new List<string>();
                
                foreach (string toLoad in finalListToLoad)
                {
                    try
                    {
#if NET461
                    
                        var assembly = Assembly.LoadFile(toLoad);
                        allAssemblies.Add(assembly);
#else
                        var assembly = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(toLoad);
                        allAssemblies.Add(assembly);
#endif
                        if (dlls.Contains(toLoad))
                        {
                            searchableAssemblies.Add(assembly);
                        }

                    }
                    catch (Exception ex)
                    {
                    }
                }

                var conventionsAssembly = allAssemblies.FirstOrDefault(x => x.GetName().Name == "GraphQL.Conventions");
                // load the dlll + all other dlls from the

                // we want to see if we have loaded in graphql.conventions

                if (conventionsAssembly != null)
                {
                    var builder = new GraphQLConventionsRequestHandlerBuilder(conventionsAssembly);
                    builder.AddQueries(query.Values ?? Enumerable.Empty<string>(), searchableAssemblies);
                    builder.AddMutations(mutation.Values ?? Enumerable.Empty<string>(), searchableAssemblies);
                    var schema = builder.GenerateSchema();

                    if (output.HasValue())
                    {
                        var fullOutputPath = Path.GetFullPath(output.Value());
                        Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath));
                        File.WriteAllText(fullOutputPath, schema);
                    }
                    else
                    {
                        Console.Write(schema);
                    }
                    return 0;
                }
                else
                {
                    Console.Error.WriteLine("currently only support extract schemas from assemblies using GraphQL.Conventions");
                    return -1;
                }

            });
        }

    }

#if !NET461
    static class AssemblyLoader
    {
        static Dictionary<string, Assembly> cache = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
        public static (Assembly assembly, IEnumerable<Assembly> allAssemblies) Load(string assemblyFullPath)
        {
            if (cache.ContainsKey(assemblyFullPath))
            {
                var a = cache[assemblyFullPath];
                return (a, Enumerable.Empty<Assembly>());
            }

            var profileFolder = System.Environment.GetEnvironmentVariable("USERPROFILE");
            var programFiles = Environment.GetEnvironmentVariable("PROGRAMFILES");
            var probFolders = new[] {
                Path.Combine(profileFolder, ".nuget\\packages"),
                Path.Combine(programFiles, "dotnet\\sdk\\NuGetFallbackFolder"),
            };
            var extensions = new[] { ".dll", ".exe" };

            var assembly = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyFullPath);
            cache.Add(assemblyFullPath, assembly);
            var context = DependencyContext.Load(assembly);
            if (context != null)
            {
                var additional = context.RuntimeLibraries
                    .SelectMany(x =>
                    {
                        if (x.Path != null)
                        {
                            var paths = x.RuntimeAssemblyGroups.FirstOrDefault()?.AssetPaths;
                            if (paths != null)
                            {
                                var folders = probFolders
                                            .Select(p => Path.Combine(p, x.Path));
                                var files = paths.SelectMany(p => folders.Select(f => Path.Combine(f, p))).Select(p => Path.GetFullPath(p));
                                var path = files.FirstOrDefault(f => File.Exists(f));
                                if (path != null)
                                {
                                    try
                                    {
                                        var res = AssemblyLoader.Load(path);
                                        return res.allAssemblies;

                                    }
                                    catch { }
                                }
                            }
                        }
                        return Enumerable.Empty<Assembly>();
                    }).Where(x => x != null).ToList();

                additional.Add(assembly);
                return (assembly, additional);
            }
            return (assembly, new List<Assembly> { assembly } );
        }
    }

    //public class AssemblyLoader : System.Runtime.Loader.AssemblyLoadContext
    //{
    //    static AssemblyLoader Default = new AssemblyLoader();

    //    // Not exactly sure about this
    //    protected override Assembly Load(AssemblyName assemblyName)
    //    {
    //        var deps = DependencyContext.Default;
    //        var res = deps.CompileLibraries.Where(d => d.Name.Contains(assemblyName.Name)).ToList();
    //        var assembly = Assembly.Load(new AssemblyName(res.First().Name));
    //        return assembly;
    //    }
    //}


#endif
}
