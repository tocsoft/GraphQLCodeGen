using System;
using System.Collections.Generic;
using System.Text;
using HotChocolate.Language;
using System.Linq;
using System.Diagnostics;

namespace Tocsoft.GraphQLCodeGen.ObjectModel
{
    [DebuggerDisplay(@"\{{Name}\}")]
    internal class EnumType : IGraphQLInitter, IGraphQLType
    {
        private EnumTypeDefinitionNode op;

        public EnumType(EnumTypeDefinitionNode op)
        {
            this.op = op;
        }

        public string Name => this.op.Name.Value;
        public IEnumerable<string> Values => this.op.Values.Select(x => x.Name.Value);

        public void Resolve(GraphQLDocument doc)
        {
            
        }
    }
}
