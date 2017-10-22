using System;
using System.Collections.Generic;
using System.Text;
using GraphQLParser.AST;
using System.Linq;
using Tocsoft.GraphQLCodeGen.ObjectModel.Selections;

namespace Tocsoft.GraphQLCodeGen.ObjectModel
{
    internal class Operation : IGraphQLInitter
    {
        private GraphQLOperationDefinition operation;

        public string Name { get; set; }
        public IDictionary<string, ValueType> Paramaters { get; set; }

        public SetSelection Selection { get; set; }
        public string Query { get; private set; }
        public string Path { get; private set; }

        public IEnumerable<SetSelection> DecendentsAndSelf()
        {
            yield return Selection;
            foreach (var s in Selection.Fields.SelectMany(f => f.DecendentsAndSelf()))
            {
                yield return s;
            }
        }

        public Operation(GraphQLOperationDefinition op)
        {
            this.operation = op;

            Name = op.Name?.Value;

            Selection = new SetSelection(operation.SelectionSet);
        }

        public void Resolve(GraphQLDocument doc)
        {
            Paramaters = operation.VariableDefinitions.ToDictionary(x => x.Variable.Name.Value, x => doc.ResolveValueType(x.Type));

            var rootType = doc.ResolveType(operation.Operation) as IGraphQLFieldCollection;
           
            (this.Query, this.Path) = doc.ResolveQuery(operation.Location);

            Selection.Resolve(doc, rootType);
        }

       
    }
}
