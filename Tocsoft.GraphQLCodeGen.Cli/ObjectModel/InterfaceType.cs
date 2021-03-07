using HotChocolate.Language;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Tocsoft.GraphQLCodeGen.ObjectModel
{
    [DebuggerDisplay("[{Name}]")]
    internal class InterfaceType : IGraphQLInitter, IGraphQLType, IGraphQLFieldCollection
    {
        private readonly InterfaceTypeDefinitionNode definition;

        public string Name { get; set; }

        public IEnumerable<Field> Fields { get; set; }

        public InterfaceType(InterfaceTypeDefinitionNode definition) {
            this.definition = definition;
            this.Name = definition.Name?.Value;
        }

        public void Resolve(GraphQLDocument doc)
        {
            this.Fields = this.definition.Fields.Select(x => new Field(x)).ToList();
            foreach (Field f in this.Fields) { f.Resolve(doc); }
        }
    }
}
