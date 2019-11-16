namespace Tocsoft.GraphQLCodeGen.Cli
{
    public interface ILogger
    {
        void Error(string str);
        void Message(string str);
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
}
