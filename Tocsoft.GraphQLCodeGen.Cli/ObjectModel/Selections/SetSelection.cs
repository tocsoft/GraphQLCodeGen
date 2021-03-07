using System;
using System.Collections.Generic;
using System.Text;
using HotChocolate.Language;
using System.Linq;

namespace Tocsoft.GraphQLCodeGen.ObjectModel.Selections
{
    // this is used to build to ResultObjectTypes
    internal class SetSelection: IGraphQLASTNodeLinked
    {
        private NamedTypeNode TypeCondition { get; set; }
        private SelectionSetNode op;
        public IGraphQLType RootType { get; set; }
        public IValueNode<string> SpecifiedTypeName { get; set; }

        public string UniqueIdentifier { get; set; }
        public IEnumerable<FieldSelection> Fields => fields;
        
        /// <summary>
        /// this makes up the list of named interfaces that we will be applying
        /// </summary>
        public IEnumerable<FragmentType> Fragments => fragmentsItems;

        ISyntaxNode IGraphQLASTNodeLinked.ASTNode => op;

        private List<FragmentType> fragmentsItems = new List<FragmentType>();
        private List<NameNode> fragmentNames;
        private List<SetSelection> inlineFragments = new List<SetSelection>();
        private List<SetSelection> fragments = new List<SetSelection>();
        private List<FieldSelection> fields = new List<FieldSelection>();

        public SetSelection(SelectionSetNode op)
        {
            this.op = op;
            List<object> nodes = op.Selections.Select(this.Visit).ToList();
            this.fields.AddRange(nodes.OfType<FieldSelection>());
            this.inlineFragments.AddRange(nodes.OfType<SetSelection>());
            this.fragmentNames = nodes.OfType<NameNode>().ToList();
        }

        public SetSelection(InlineFragmentNode op)
            : this(op.SelectionSet)
        {
            this.TypeCondition = op.TypeCondition;
        }

        private object Visit(ISyntaxNode node)
        {
            switch (node)
            {
                case HotChocolate.Language.FieldNode op:
                    return new FieldSelection(op);
                case InlineFragmentNode op:
                    return new SetSelection(op);
                case FragmentSpreadNode op:
                    return op.Name;
                default:
                    return node;
            }
        }

        internal void Resolve(GraphQLDocument doc, IGraphQLType rootType, IValueNode<string> specifiedTypeName = null)
        {
            this.RootType = rootType;

            foreach (FieldSelection f in this.Fields)
            {
                f.Resolve(doc, rootType as IGraphQLFieldCollection);
            }

            foreach (SetSelection f in this.inlineFragments)
            {
                IGraphQLType root = doc.ResolveType(f.TypeCondition);
                f.Resolve(doc, root);
                this.fragments.Add(f);
            }

            // do these after the inline fragments as they have been pre-resolved
            foreach (NameNode name in this.fragmentNames)
            {
                var fragmentRoot = doc.ResolveType(name.Value) as FragmentType;
                this.fragmentsItems.Add(fragmentRoot);
                this.fragments.Add(fragmentRoot.Selection);
            }

            // merge fields from fragments into root object
            foreach (SetSelection f in this.fragments)
            {
                var currentFieldRefs = fields.Select(x => x.UniqueIdentifier);
                var newFields = f.Fields.Where(x => !currentFieldRefs.Contains(x.UniqueIdentifier));
                fields.AddRange(newFields);
            }

            this.SpecifiedTypeName = specifiedTypeName;

            this.UniqueIdentifier = $"{this.TypeCondition?.Name?.Value}_{rootType?.Name}_{string.Join("|", this.Fields.Select(x => x.UniqueIdentifier))}_{string.Join("|", this.fragments.Select(x => x.UniqueIdentifier))}";
        }
    }
}
