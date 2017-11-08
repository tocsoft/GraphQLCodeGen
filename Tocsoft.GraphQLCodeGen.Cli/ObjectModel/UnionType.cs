using System;
using System.Collections.Generic;
using System.Text;
using GraphQLParser.AST;
using System.Linq;

namespace Tocsoft.GraphQLCodeGen.ObjectModel
{
    internal class UnionType : IGraphQLInitter, IGraphQLType
    {
        private GraphQLUnionTypeDefinition op;

        public UnionType(GraphQLUnionTypeDefinition op)
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
