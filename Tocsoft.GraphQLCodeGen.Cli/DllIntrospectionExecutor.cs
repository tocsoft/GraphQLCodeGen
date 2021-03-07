
#if DLL_INTROSPECTION
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace Tocsoft.GraphQLCodeGen
{
    internal static class DllIntrospectionExecutor
    {
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
                    var res = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(x =>
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
                    catch
                    {
                    }
                }

                var hcLib = allAssemblies.FirstOrDefault(x => x.GetName().Name == "HotChocolate.Execution");
                var abstractionsAssembly = allAssemblies.FirstOrDefault(x => x.GetName().Name == "Microsoft.Extensions.Hosting.Abstractions");
                var hostBuilderType = abstractionsAssembly?.GetType("Microsoft.Extensions.Hosting.IHostBuilder");
                var hostBuilder_BuildMethod = hostBuilderType?.GetMethod("Build");
                var hostType = hostBuilder_BuildMethod?.ReturnType;
                var serviceProviderProp = hostType?.GetProperty("Services");
                var serviceProviderType = serviceProviderProp?.PropertyType;
                var GetServiceMethod = serviceProviderType?.GetMethod("GetService", new Type[] { typeof(Type) });
                var executerType = hcLib?.GetType("HotChocolate.Execution.IRequestExecutorResolver");
                var GetRequestExecutorAsyncMethod = executerType?.GetMethod("GetRequestExecutorAsync");

                var ValueTask_IRequestExecutor = GetRequestExecutorAsyncMethod?.ReturnType;
                var returnValue = ValueTask_IRequestExecutor?.GetProperty("Result");
                var IRequestExecutorType = returnValue?.PropertyType;
                var schemaProprety = IRequestExecutorType?.GetProperty("Schema");

                // load the dlll + all other dlls from the

                // we want to see if we have loaded in graphql.conventions

                bool hasSchema = false;
                string schema = "";
                if (hcLib != null && abstractionsAssembly != null && GetServiceMethod != null && schemaProprety != null)
                {
                    var sourceDlls = allAssemblies.Where(x => dlls.Contains(x.Location));
                    foreach (var dll in sourceDlls)
                    {
                        var entryClass = dll.EntryPoint.DeclaringType;
                        var method = entryClass.GetMethod("CreateHostBuilder", new Type[] { typeof(string[]) });
                        if (method != null)
                        {
                            var hostBuilder = method.Invoke(null, new object[] { new string[0] });

                            var host = hostBuilder_BuildMethod.Invoke(hostBuilder, null);
                            var provider = serviceProviderProp.GetMethod.Invoke(host, null);
                            var executer = GetServiceMethod.Invoke(provider, new object[] { executerType });

                            if (executer != null)
                            {
                                var executeorTask = GetRequestExecutorAsyncMethod.Invoke(executer, null);
                                var executor = returnValue.GetMethod.Invoke(executeorTask, null);
                                var schemaObj = schemaProprety.GetMethod.Invoke(executor, null);
                                schema = schemaObj.ToString();
                                hasSchema = true;
                                break;
                            }
                        }
                    }
                }

                if (!hasSchema)
                {
                    Console.Error.WriteLine("HotChocolate and uses the aspnetcore generic host conventions of exposing a `CreateHostBuilder` method from the same class as the `Main` method.");
                    return -1;
                }

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
            return (assembly, new List<Assembly> { assembly });
        }
    }

    //public class PLuginContext : AssemblyLoadContext
    //{
    //    private readonly AssemblyDependencyResolver resolver;

    //    public static Assembly Load(string path)
    //    {
    //        var ctx = new PLuginContext(path);
    //        this.LoadFromAssemblyPath(rootAssembly);
    //    }
    //    public PLuginContext(string rootAssembly)
    //    {
    //        this.resolver = new System.Runtime.Loader.AssemblyDependencyResolver(rootAssembly);

    //    }

    //    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    //    {
    //        var path = this.resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
    //        return this.LoadUnmanagedDllFromPath(path);
    //    }

    //    protected override Assembly Load(AssemblyName assemblyName)
    //    {
    //        var path = this.resolver.ResolveAssemblyToPath(assemblyName);
    //        return AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
    //    }

    //}

#endif
}

#endif
