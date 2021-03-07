using System;
using System.Collections.Generic;
using System.Text;
using HotChocolate.Language;

namespace Tocsoft.GraphQLCodeGen.ObjectModel
{
    internal class Argument : IGraphQLInitter
    {
        private InputValueDefinitionNode definition;

        public string Name { get; private set; }
        public string DefaultValue { get; private set; }
        public ValueTypeReference Type { get; private set; }

        public Argument(InputValueDefinitionNode definition)
        {
            this.definition = definition;
            this.Name = definition.Name.Value;
            
        }

        public void Resolve(GraphQLDocument doc)
        {
            this.Type = doc.ResolveValueType(this.definition.Type);

            //if(this.definition.DefaultValue is ScalerValues sv)!!!
            //{
            //    this.DefaultValue = sv.Value;
            //}
            //else
            {
                this.DefaultValue = this.definition.DefaultValue?.ToString();
            }


            this.Type = doc.ResolveValueType(this.definition.Type);
        }

        public override string ToString()
        {
            string v = $"{this.Name} : {this.Type}";
            if(this.DefaultValue != null)
            {
                v = $"{v} = {this.DefaultValue}";
            }
            return v;
        }
    }
}
