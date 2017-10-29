using System;
using System.Collections.Generic;
using System.Text;
using GraphQLParser.AST;

namespace Tocsoft.GraphQLCodeGen.ObjectModel
{
    internal class Argument : IGraphQLInitter
    {
        private GraphQLInputValueDefinition definition;

        public string Name { get; private set; }
        public string DefaultValue { get; private set; }
        public ValueTypeReference Type { get; private set; }

        public Argument(GraphQLInputValueDefinition definition)
        {
            this.definition = definition;
            this.Name = definition.Name.Value;
            
        }

        public void Resolve(GraphQLDocument doc)
        {
            this.Type = doc.ResolveValueType(definition.Type);

            if(definition.DefaultValue is GraphQLParser.AST.GraphQLScalarValue sv)
            {
                DefaultValue = sv.Value;
            }
            else
            {
                DefaultValue = definition.DefaultValue?.ToString();
            }


            this.Type = doc.ResolveValueType(definition.Type);
        }

        public override string ToString()
        {
            var v = $"{Name} : {Type}";
            if(DefaultValue != null)
            {
                v = $"{v} = {DefaultValue}";
            }
            return v;
        }
    }
}
