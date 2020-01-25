using GraphQLParser.AST;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Tocsoft.GraphQLCodeGen.ObjectModel.Selections;

namespace Tocsoft.GraphQLCodeGen.ObjectModel
{
    [DebuggerDisplay(@"... \{{Name}\}")]
    internal class FragmentType : IGraphQLType, IGraphQLInitter
    {
        private readonly GraphQLFragmentDefinition definition;
        private IGraphQLFieldCollection rootType;

        public string Name { get; set; }

        public SetSelection Selection { get; set; }

        public string Query { get; private set; }

        public string Path { get; private set; }

        public FragmentType(GraphQLFragmentDefinition definition)
        {
            this.definition = definition;
            this.Name = definition.Name?.Value;
            this.Selection = new SetSelection(this.definition);
        }

        public void Resolve(GraphQLDocument doc)
        {
            this.rootType = doc.ResolveType(this.definition.TypeCondition) as IGraphQLFieldCollection;
            this.Selection.Resolve(doc, this.rootType);
            this.Path = doc.ResolveQuerySource(this.definition.Location);
            this.Query = doc.ResolveFragment(this.definition);
        }
    }
}
