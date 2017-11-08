using GraphQLParser.AST;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Tocsoft.GraphQLCodeGen.ObjectModel.Selections;

namespace Tocsoft.GraphQLCodeGen.ObjectModel
{
    [DebuggerDisplay(@"\{{Name}\}")]
    internal class ObjectType : IGraphQLType, IGraphQLInitter, IGraphQLFieldCollection
    {
        private readonly GraphQLObjectTypeDefinition definition;
        private readonly GraphQLInputObjectTypeDefinition definitionInput;
        private GraphQLSelectionSet selectionSet;
        private IGraphQLFieldCollection rootType;

        public string Name { get; set; }

        public IEnumerable<InterfaceType> Interfaces { get; set; }
        public IEnumerable<Field> Fields { get; private set; }

        public ObjectType(GraphQLObjectTypeDefinition definition)
        {
            this.definition = definition;
            this.Name = definition.Name?.Value;
        }

        public ObjectType(GraphQLInputObjectTypeDefinition definition)
        {
            this.definitionInput = definition;
            this.Name = definition.Name?.Value;
        }
        


        public void Resolve(GraphQLDocument doc)
        {
            if (this.definition != null)
            {
                this.Interfaces = ResolveInterfaces(doc).ToList();
                this.Fields = this.definition.Fields.Select(x =>
                {
                    return new Field(x);
                }).ToList();
            }
            else            if (this.definitionInput != null)
            {
                this.Fields = this.definitionInput.Fields.Select(x =>
                {
                    return new Field(x);
                }).ToList();
            }

            foreach (Field f in this.Fields) { f.Resolve(doc); }

        }
        private IEnumerable<InterfaceType> ResolveInterfaces(GraphQLDocument doc)
        {
            foreach (GraphQLNamedType interfaceDefinitionLookup in this.definition.Interfaces)
            {
                IGraphQLType actualinterface = doc.ResolveType(interfaceDefinitionLookup);
                yield return actualinterface as InterfaceType;
            }
        }
    }
}
