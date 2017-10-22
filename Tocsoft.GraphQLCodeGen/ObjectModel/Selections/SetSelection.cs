using System;
using System.Collections.Generic;
using System.Text;
using GraphQLParser.AST;
using System.Linq;

namespace Tocsoft.GraphQLCodeGen.ObjectModel.Selections
{
    // this is used to build to ResultObjectTypes
    internal class SetSelection
    {
        private GraphQLSelectionSet op;
        public IGraphQLFieldCollection RootType { get; set; }

        public string UniqueIdentifier { get; set; }
        public IEnumerable<FieldSelection> Fields { get; set; }

        public SetSelection(GraphQLSelectionSet op)
        {
            this.op = op;
            var nodes = op.Selections.Select(Visit).ToList();
            this.Fields = nodes.OfType<FieldSelection>().ToList();
        }

        private object Visit(GraphQLParser.AST.ASTNode node)
        {
            switch (node)
            {
                case GraphQLFieldSelection op:
                    return new FieldSelection(op);
                default:
                    return node;
            }
        }

        internal void Resolve(GraphQLDocument doc, IGraphQLFieldCollection rootType)
        {
            this.RootType = rootType;
            foreach (var f in Fields)
            {
                f.Resolve(doc, rootType);
            }

            UniqueIdentifier = $"{rootType.Name}_{string.Join("|", Fields.Select(x => x.UniqueIdentifier))}";
        }
    }
}
