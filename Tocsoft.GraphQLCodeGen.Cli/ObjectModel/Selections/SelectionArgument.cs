using System;
using System.Collections.Generic;
using System.Text;
using GraphQLParser.AST;
using System.Linq;

namespace Tocsoft.GraphQLCodeGen.ObjectModel.Selections
{
    internal class SelectionArgument
    {
        private GraphQLArgument op;

        public string Name { get; set; }
        public SelectionArgument(GraphQLArgument op)
        {
            this.op = op;
            this.Name = op.Name.Value;
        }

        internal void Resolve(IGraphQLFieldCollection rootType)
        {
            
        }
    }
}
