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
            List<object> nodes = op.Selections.Select(this.Visit).ToList();
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
            foreach (FieldSelection f in this.Fields)
            {
                f.Resolve(doc, rootType as IGraphQLFieldCollection);
            }

            foreach (SetSelection f in this.Fragments)
            {
                IGraphQLType root = doc.ResolveType(f.TypeCondition);
                f.Resolve(doc, root);
            }

            this.UniqueIdentifier = $"{this.TypeCondition?.Name?.Value}_{rootType?.Name}_{string.Join("|", this.Fields.Select(x => x.UniqueIdentifier))}_{string.Join("|", this.Fragments.Select(x => x.UniqueIdentifier))}";
        }
    }
}
