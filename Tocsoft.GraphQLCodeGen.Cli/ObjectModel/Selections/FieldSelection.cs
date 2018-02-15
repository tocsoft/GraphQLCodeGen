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

        public string UniqueIdentifier => $"{this.Name}_{this.ScalerType}{this.Selection?.UniqueIdentifier}";
        public string Name { get; set; }
        private  WellknownScalarType? ScalerType => (this.Type?.Type.Type as ScalarType)?.WellknownType;
        public Field Type { get; set; }
        public SetSelection Selection { get; set; }

        public IEnumerable<SetSelection> DecendentsAndSelf()
        {
            if(this.Selection != null){
                yield return this.Selection;
                foreach (SetSelection s in this.Selection.Fields.SelectMany(f => f.DecendentsAndSelf()))
                {
                    yield return s;
                }
            }
        }

        public FieldSelection(GraphQLFieldSelection op)
        {
            this.op = op;
            this.Name = op.Alias?.Value ?? op.Name.Value;
            if (op.SelectionSet != null)
            {
                this.Selection = new SetSelection(op.SelectionSet);
            }

            // to deduplocate part sof 
        }

        internal bool Resolve(GraphQLDocument doc, IGraphQLFieldCollection rootType)
        {
            if (this.op.Name.Value == "__typename")
            {
                // special case here
                this.Type = Field.TypeName(doc);
            }
            else
            {
                // this is special we need to treat it as such
                this.Type = rootType.Fields.SingleOrDefault(x => x.Name == this.op.Name.Value);
                if(this.Type == null)
                {
                    doc.AddError(ErrorCodes.UnknownField, $"The field '{this.op.Name.Value}' in not a valid member of '{rootType.Name}'", this.op);
                    return false;
                }
            }
            if(this.Selection != null)
            {
                IGraphQLType root = this.Type.Type.Type as IGraphQLType;
                this.Selection.Resolve(doc, root);
            }

            //if(selection != null)
            //{
            //    // if we have subselction then it must be an object type really, unless an interface will work instead ???
            //    selection.Resolve(FieldType.Type.Type as IGraphQLFieldCollection);
            //}
            return true;
        }
    }
}
