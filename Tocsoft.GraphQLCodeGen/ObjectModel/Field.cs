﻿using System;
using System.Collections.Generic;
using System.Text;
using GraphQLParser.AST;
using System.Linq;
using System.Diagnostics;

namespace Tocsoft.GraphQLCodeGen.ObjectModel
{
    internal class Field : IGraphQLInitter
    {
        internal static Field TypeName(GraphQLDocument doc)
        {
            var field = new Field("__typename");
            field.Type = new ValueTypeReference
            {
                Type = doc.ResolveType("String"),
                CanCollectionBeNull = false,
                CanValueBeNull = false,
                IsCollection = false
            };
            return field;
        }

        private GraphQLFieldDefinition definition;
        private GraphQLInputValueDefinition definitionInput;

        public string Name { get; private set; }
        public IEnumerable<Argument> Arguments { get; private set; }

        public bool IsMethod => Arguments.Any();

        public ValueTypeReference Type { get; set; }
        private Field(string name)
        {
            this.Name = name;
            this.Arguments = Enumerable.Empty<Argument>();
        }
        public Field(GraphQLFieldDefinition definition)
        {
            this.definition = definition;
            this.Name = definition.Name?.Value;
            this.Arguments = definition.Arguments.Select(x => new Argument(x)).ToList();
        }

        public Field(GraphQLInputValueDefinition definitionInput)
        {
            this.definitionInput = definitionInput;
            this.Name = definitionInput.Name?.Value;
            this.Arguments = Enumerable.Empty<Argument>();
        }

        public void Resolve(GraphQLDocument doc)
        {
            if (this.Name == "__typename") { return; }

            if (definition != null)
            {

                Type = doc.ResolveValueType(definition.Type);
            }
            else if (definitionInput != null)
            {
                Type = doc.ResolveValueType(definitionInput.Type);
            }

            foreach (var a in Arguments)
            {
                a.Resolve(doc);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(this.Name);
            if (this.IsMethod)
            {
                sb.Append("(");
                var first = true;
                foreach (var p in this.Arguments)
                {
                    if (!first) sb.Append(", ");
                    first = false;
                    sb.Append(p.ToString());
                }
                sb.Append(")");
            }
            sb.Append(" : ");
            sb.Append(this.Type.ToString());

            return base.ToString();
        }
    }
}
