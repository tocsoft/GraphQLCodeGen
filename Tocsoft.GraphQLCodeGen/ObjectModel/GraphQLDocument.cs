using Tocsoft.GraphQLCodeGen.ObjectModel.Selections;
using GraphQLParser.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Tocsoft.GraphQLCodeGen.IntrospectedSchemeParser;

namespace Tocsoft.GraphQLCodeGen.ObjectModel
{
    internal class GraphQLDocument
    {
        public GraphQLDocument(GraphQLParser.AST.GraphQLDocument ast, IEnumerable<LocatedNamedSource> queryParts)
        {
            this.ast = ast;

            this.QueryParts = queryParts;
            var items = ast.Definitions.Select(Visit).ToList();
            this.Operations = items.OfType<Operation>().ToList();
            this.types = items.OfType<IGraphQLType>().OrderBy(x=>x.Name).ToList();

            foreach (var i in items.OfType<IGraphQLInitter>().Where(x => !(x is Operation)))
            {
                i.Resolve(this);
            }
            foreach (var i in items.OfType<Operation>())
            {
                i.Resolve(this);
            }
        }

        public IEnumerable<Operation> Operations { get; }

        private readonly GraphQLParser.AST.GraphQLDocument ast;

        public IEnumerable<LocatedNamedSource> QueryParts { get; private set; }

        private List<IGraphQLType> types;

        internal ValueTypeReference ResolveValueType(GraphQLType type)
        {
            var result = new ValueTypeReference();
            UnPackType(type, result);

            return result;
        }

        internal (string query, string filename) ResolveQuery(GraphQLLocation location)
        {
            var part = QueryParts.Where(x => x.StartAt <= location.Start).OrderByDescending(x => x.StartAt).First();

            var offsetStart = location.Start - part.StartAt;
            var length = location.End - location.Start;
            if (length + offsetStart > part.Body.Length)
            {
                length = part.Body.Length - offsetStart;
            }

            var text = part.Body.Substring(offsetStart, length);

            return (text, part.Path);
        }

        internal IGraphQLType ResolveType(GraphQLParser.AST.OperationType type)
        {
            return types.Where(x => x.Name == type.ToString()).Single();
        }

        internal IGraphQLType ResolveType(GraphQLNamedType type)
        {
            return ResolveType(type.Name.Value);
        }

        internal IGraphQLType ResolveType(string typeName)
        {
            var result = types.SingleOrDefault(x => x.Name == typeName);

            if (result == null)
            {
                WellknownScalarType wellknownType;
                if (!Enum.TryParse(typeName, out wellknownType))
                {
                    wellknownType = WellknownScalarType.OTHER;
                }
                result = new ScalarType()
                {
                    Name = typeName,
                    WellknownType = wellknownType
                };
                types.Add(result);

            }

            return result;
        }


        private void UnPackType(GraphQLType type, ValueTypeReference target)
        {
            try
            {
                if (type is GraphQLNonNullType nonNullType)
                {
                    target.CanValueBeNull = true;
                    UnPackType(nonNullType.Type, target);
                }
                else if (type is GraphQLListType listType)
                {
                    target.IsCollection = true;
                    if (target.CanValueBeNull)
                    {
                        target.CanValueBeNull = false;
                        target.CanCollectionBeNull = true;
                    }
                    UnPackType(listType.Type, target);
                }
                else if (type is GraphQLNamedType namedType)
                {
                    target.Type = ResolveType(namedType);
                }
                else
                {
                    throw new Exception("dunno???");
                }
            }catch(Exception ex)
            {
                throw;
            }
        }
        private object VisitSelectionSet(GraphQLParser.AST.ASTNode node)
        {
            switch (node)
            {
                case GraphQLFieldSelection op:
                    return new FieldSelection(op);
                default:
                    return node;
            }
        }
        private object Visit(GraphQLParser.AST.ASTNode node)
        {
            switch (node)
            {
                case GraphQLOperationDefinition op:
                    return new Operation(op);
                case GraphQLInterfaceTypeDefinition op:
                    return new InterfaceType(op);
                case GraphQLObjectTypeDefinition op:
                    return new ObjectType(op);
                case GraphQLEnumTypeDefinition op:
                    return new EnumType(op);
                case GraphQLUnionTypeDefinition op:
                    return new UnionType(op);
                case GraphQLInputObjectTypeDefinition op:
                    return new ObjectType(op);
                default:
                    return null;
            }
        }

    }

    internal class ScalarType : IGraphQLType
    {
        public string Name { get; set; }
        public WellknownScalarType? WellknownType { get; set; }
    }

    internal enum WellknownScalarType
    {
        OTHER,
        Int,
        Float,
        String,
        Boolean,
        ID
    }

    internal class ValueTypeReference
    {
        public bool CanValueBeNull { get; set; }
        public bool IsCollection { get; set; }
        public bool CanCollectionBeNull { get; set; }
        public IGraphQLType Type { get; set; }

        public override string ToString()
        {
            var val = Type.Name;
            if (CanValueBeNull)
            {
                val += "!";
            }
            if (IsCollection)
            {
                val = "[" + val + "]";
            }
            if (CanCollectionBeNull)
            {
                val += "!";
            }

            return val;
        }
    }
}
