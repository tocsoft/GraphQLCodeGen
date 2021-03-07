using System;
using System.Collections.Generic;
using System.Text;
using HotChocolate.Language;
using System.Linq;

namespace Tocsoft.GraphQLCodeGen.ObjectModel.Selections
{
    internal class SelectionArgument
    {
        private ArgumentNode op;

        public string Name { get; set; }
        public SelectionArgument(ArgumentNode op)
        {
            this.op = op;
            this.Name = op.Name.Value;
        }

        internal void Resolve(IGraphQLFieldCollection rootType)
        {
            
        }
    }
}
