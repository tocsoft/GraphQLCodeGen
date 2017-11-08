using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Tocsoft.GraphQLCodeGen.Cli;
using static Tocsoft.GraphQLCodeGen.CodeGeneratorSettingsLoader;
using System.Diagnostics;

namespace Tocsoft.GraphQLCodeGen.SchemaIntrospection
{
    public class DllIntrospection : IIntrosepctionProvider
    {
        public SchemaSource.SchemaTypes SchemaType => SchemaSource.SchemaTypes.Dll;


        public Task<string> LoadSchema(SchemaSource source)
        {
            var path = new Uri(typeof(Program).GetTypeInfo().Assembly.CodeBase).LocalPath;
            var exe = path;
            var query = string.Join(" ", (source.QueryType ?? Enumerable.Empty<string>()).Select(x => $"--query \"{x}\""));
            var mutations = string.Join(" ", (source.MutationType ?? Enumerable.Empty<string>() ).Select(x => $"--mutation \"{x}\""));
            var args = $"introspect \"{source.Location}\" {query} {mutations}";
            
            if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                args = $"\"{exe}\" {args}";
                exe = "dotnet";
            }
            try
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    RedirectStandardOutput = true,
                    Arguments = args,
                    FileName = exe,
                    UseShellExecute = false
                });

                process.WaitForExit();
                var schema = process.StandardOutput.ReadToEnd();

                return Task.FromResult(schema);
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }
    }
}
