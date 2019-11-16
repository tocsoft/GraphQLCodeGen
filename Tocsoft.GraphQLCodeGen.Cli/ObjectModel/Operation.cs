using System;
using System.Collections.Generic;
using System.Text;
using GraphQLParser.AST;
using System.Linq;
using Tocsoft.GraphQLCodeGen.ObjectModel.Selections;

namespace Tocsoft.GraphQLCodeGen.ObjectModel
{
    internal class Operation : IGraphQLInitter, IGraphQLASTNodeLinked
    {
        private GraphQLOperationDefinition operation;

        public string Name { get; set; }
        public IDictionary<string, ValueTypeReference> Paramaters { get; set; }

        public SetSelection Selection { get; set; }
        public string Query { get; private set; }
        public string Path { get; private set; }

        ASTNode IGraphQLASTNodeLinked.ASTNode => operation;

        public IEnumerable<SetSelection> DecendentsAndSelf()
        {
            yield return this.Selection;
            foreach (SetSelection s in this.Selection.Fields.SelectMany(f => f.DecendentsAndSelf()))
            {
                yield return s;
            }
        }

        public Operation(GraphQLOperationDefinition op)
        {
            this.operation = op;

            this.Name = op.Name?.Value;

            this.Selection = new SetSelection(this.operation.SelectionSet);
        }

        public void Resolve(GraphQLDocument doc)
        {
            this.Paramaters = this.operation.VariableDefinitions?.ToDictionary(x => x.Variable.Name.Value, x => doc.ResolveValueType(x.Type)) ?? new Dictionary<string, ValueTypeReference>();

            IGraphQLFieldCollection rootType = doc.ResolveType(this.operation.Operation) as IGraphQLFieldCollection;
           
            this.Path = doc.ResolveQuerySource(this.operation.Location);
            this.Query = doc.ResolveQuery(this.operation);

            this.Selection.Resolve(doc, rootType);
        }

    }
}
