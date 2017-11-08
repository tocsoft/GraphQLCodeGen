using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static Tocsoft.GraphQLCodeGen.CodeGeneratorSettingsLoader;
using static Tocsoft.GraphQLCodeGen.CodeGeneratorSettingsLoader.SchemaSource;

namespace Tocsoft.GraphQLCodeGen.SchemaIntrospection
{
    public interface IIntrosepctionProvider
    {
        SchemaTypes SchemaType { get; }
        Task<string> LoadSchema(SchemaSource source);
    }
}
