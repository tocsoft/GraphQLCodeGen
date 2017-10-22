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
        private GraphQLNamedType TypeCondition { get; set; }
        private GraphQLSelectionSet op;
        public IGraphQLType RootType { get; set; }

        public string UniqueIdentifier { get; set; }
        public IEnumerable<FieldSelection> Fields { get; set; }
        public IEnumerable<SetSelection> Fragments { get; set; }
        
        public SetSelection(GraphQLSelectionSet op)
        {
            this.op = op;
            var nodes = op.Selections.Select(Visit).ToList();
            this.Fields = nodes.OfType<FieldSelection>().ToList();
            this.Fragments = nodes.OfType<SetSelection>().ToList();
        }

        public SetSelection(GraphQLInlineFragment op)
            :this(op.SelectionSet)
        {
            this.TypeCondition = op.TypeCondition;
        }

        private object Visit(GraphQLParser.AST.ASTNode node)
        {
            switch (node)
            {
                case GraphQLFieldSelection op:
                    return new FieldSelection(op);
                case GraphQLInlineFragment op:
                    return new SetSelection(op);
                default:
                    return node;
            }
        }

        internal void Resolve(GraphQLDocument doc, IGraphQLType rootType)
        {
            this.RootType = rootType;
            foreach (var f in Fields)
            {
                f.Resolve(doc, rootType as IGraphQLFieldCollection);
            }

            foreach (var f in Fragments)
            {
                var root = doc.ResolveType(f.TypeCondition);
                f.Resolve(doc, root);
            }

            UniqueIdentifier = $"{this.TypeCondition?.Name?.Value}_{rootType?.Name}_{string.Join("|", Fields.Select(x => x.UniqueIdentifier))}_{string.Join("|", Fragments.Select(x => x.UniqueIdentifier))}";
        }
    }
}
