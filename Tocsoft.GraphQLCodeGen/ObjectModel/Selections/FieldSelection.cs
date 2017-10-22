using System;
using System.Collections.Generic;
using System.Text;
using GraphQLParser.AST;
using System.Linq;

namespace Tocsoft.GraphQLCodeGen.ObjectModel.Selections
{
    internal class FieldSelection 
    {
        private GraphQLFieldSelection op;

        public string UniqueIdentifier => $"{Name}_{ScalerType}{Selection?.UniqueIdentifier}";
        public string Name { get; set; }
        private  WellknownScalarType? ScalerType => (Type.Type.Type as ScalarType)?.WellknownType;
        public Field Type { get; set; }
        public SetSelection Selection { get; set; }

        public IEnumerable<SetSelection> DecendentsAndSelf()
        {
            if(Selection != null){
                yield return Selection;
                foreach (var s in Selection.Fields.SelectMany(f => f.DecendentsAndSelf()))
                {
                    yield return s;
                }
            }
        }

        public FieldSelection(GraphQLFieldSelection op)
        {
            this.op = op;
            Name = op.Alias?.Value ?? op.Name.Value;
            if (op.SelectionSet != null)
            {
                Selection = new SetSelection(op.SelectionSet);
            }

            // to deduplocate part sof 
        }

        internal void Resolve(GraphQLDocument doc, IGraphQLFieldCollection rootType)
        {
            if (op.Name.Value == "__typename")
            {
                // special case here
                Type = Field.TypeName(doc);
            }
            else
            {
                // this is special we need to treat it as such
                Type = rootType.Fields.Single(x => x.Name == op.Name.Value);
            }
            if(Selection != null)
            {
                var root = Type.Type.Type as IGraphQLType;
                Selection.Resolve(doc, root);
            }

            //if(selection != null)
            //{
            //    // if we have subselction then it must be an object type really, unless an interface will work instead ???
            //    selection.Resolve(FieldType.Type.Type as IGraphQLFieldCollection);
            //}
        }
    }
}
