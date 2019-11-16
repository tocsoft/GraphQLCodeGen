using System;

namespace Tocsoft.GraphQLCodeGen.Cli
{
    public class ConsoleLogger : ILogger
    {
        private readonly bool disableStandardMessagesMessage;

        public ConsoleLogger(bool disableStandardMessagesMessage)
        {
            this.disableStandardMessagesMessage = disableStandardMessagesMessage;
        }
        public void Error(string str)
        {
            Console.Error.WriteLine(str);
        }

        public void Message(string str)
        {
            if (!this.disableStandardMessagesMessage)
            {
                Console.Out.WriteLine(str);
            }
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
}
