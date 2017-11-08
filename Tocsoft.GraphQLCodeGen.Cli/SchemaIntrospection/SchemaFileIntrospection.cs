using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Tocsoft.GraphQLCodeGen.CodeGeneratorSettingsLoader;

namespace Tocsoft.GraphQLCodeGen.SchemaIntrospection
{
    public class SchemaFileIntrospection : IIntrosepctionProvider
    {
        public SchemaSource.SchemaTypes SchemaType => SchemaSource.SchemaTypes.GraphQLSchemaFile;

        public Task<string> LoadSchema(SchemaSource source)
        {
            return Task.FromResult(File.ReadAllText(source.Location));
        }
    }
}
