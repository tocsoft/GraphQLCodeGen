using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static Tocsoft.GraphQLCodeGen.CodeGeneratorSettingsLoader;
using static Tocsoft.GraphQLCodeGen.SchemaSource;

namespace Tocsoft.GraphQLCodeGen.SchemaIntrospection
{
    internal interface IIntrosepctionProvider
    {
        SchemaTypes SchemaType { get; }
        Task<string> LoadSchema(SchemaSource source);
    }
}
