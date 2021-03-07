using System;
using System.Collections.Generic;
using System.Text;
using HotChocolate.Language;
using System.Linq;

namespace Tocsoft.GraphQLCodeGen.ObjectModel
{
    internal class UnionType : IGraphQLInitter, IGraphQLType
    {
        private UnionTypeDefinitionNode op;

        public UnionType(UnionTypeDefinitionNode op)
        {
            this.op = op;
        }

        public string Name => this.op.Name.Value;

        public IEnumerable<IGraphQLType> Types { get; private set; }

        public void Resolve(GraphQLDocument doc)
        {
            this.Types = this.op.Types.Select(x => doc.ResolveType(x)).ToList();
        }
    }
}
