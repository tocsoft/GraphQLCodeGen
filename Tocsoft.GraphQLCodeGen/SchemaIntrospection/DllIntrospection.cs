using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using static Tocsoft.GraphQLCodeGen.CodeGeneratorSettingsLoader;

namespace Tocsoft.GraphQLCodeGen.SchemaIntrospection
{
    public class DllIntrospection : IIntrosepctionProvider
    {
        public SchemaSource.SchemaTypes SchemaType => SchemaSource.SchemaTypes.Dll;


        public Task<string> LoadSchema(SchemaSource source)
        {
            // this is a recursive call back into this same exe calling the introsepction output, marked as output to standard out.
            
            return Task.FromResult(schema);
        }
    }
}
