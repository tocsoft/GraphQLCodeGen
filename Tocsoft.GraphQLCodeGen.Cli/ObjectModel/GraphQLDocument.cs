using Tocsoft.GraphQLCodeGen.ObjectModel.Selections;
using GraphQLParser.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Tocsoft.GraphQLCodeGen.IntrospectedSchemeParser;
using GraphQLParser;

namespace Tocsoft.GraphQLCodeGen.ObjectModel
{
    internal enum ErrorCodes
    {
        Unknown = 0,
        UnhandledException = 1,
        UnknownField = 2,
        SyntaxError = 3,
    }

    internal class GraphQLError
    {

        public string Message { get; set; }

        public ErrorCodes Code { get; set; }

        public string Path { get; internal set; }
        public int? Line { get; internal set; }
        public int? Column { get; internal set; }

        public override string ToString()
        {
            if (!Line.HasValue)
            {
                return $"ERROR GQL{ ((int)Code):0000}: { Message}";
            }


            return $"{Path}({Line},{Column}): ERROR GQL{((int)Code):0000}: {Message}";
        }
    }

    internal class GraphQLDocument
    {
        public static GraphQLDocument Error(ErrorCodes code, string message)
        {
            var doc = new GraphQLDocument();

            doc.AddError(code, message);
            return doc;
        }
        public static GraphQLDocument Error(GraphQLError error)
        {
            var doc = new GraphQLDocument();
            doc.errors.Add(error);
            return doc;
        }

        private GraphQLDocument()
        {

        }
        public GraphQLDocument(GraphQLParser.AST.GraphQLDocument ast, IEnumerable<LocatedNamedSource> queryParts)
        {
            this.ast = ast;

            this.QueryParts = queryParts;
            List<object> items = ast.Definitions.Select(this.Visit).ToList();
            this.Operations = items.OfType<Operation>().ToList();
            this.types = items.OfType<IGraphQLType>().OrderBy(x => x.Name).ToList();

            foreach (IGraphQLInitter i in items.OfType<IGraphQLInitter>().Where(x => !(x is Operation)))
            {
                i.Resolve(this);
            }
            foreach (Operation i in items.OfType<Operation>())
            {
                i.Resolve(this);
            }
        }
        private List<GraphQLError> errors = new List<GraphQLError>();
        public IEnumerable<GraphQLError> Errors => errors;

        internal void AddError(ErrorCodes code, string message)
        {
            errors.Add(new GraphQLError()
            {
                Code = code,
                Message = message
            });
        }


        internal void AddError(ErrorCodes code, string message, ASTNode node)
        {
            var location = node.Location;
            (LocatedNamedSource part, int offsetStart, int length) = ResolveNode(location);

            var allTextBeforeError = part.Body.Substring(0, offsetStart);
            var lines = allTextBeforeError.Split('\n');
            var line = lines.Count();
            var column = lines.Last().Length + 1;

            errors.Add(new GraphQLError()
            {
                Path = part.Path,
                Code = code,
                Message = message,
                Line = line,
                Column = column
            });
        }

        public IEnumerable<Operation> Operations { get; }

        private readonly GraphQLParser.AST.GraphQLDocument ast;

        public IEnumerable<LocatedNamedSource> QueryParts { get; private set; }

        private List<IGraphQLType> types;

        internal ValueTypeReference ResolveValueType(GraphQLType type)
        {
            ValueTypeReference result = new ValueTypeReference();
            UnPackType(type, result);

            return result;
        }

        internal (LocatedNamedSource part, int offset, int length) ResolveNode(GraphQLLocation location)
        {
            LocatedNamedSource part = this.QueryParts.Where(x => x.StartAt <= location.Start).OrderByDescending(x => x.StartAt).First();
            int offsetStart = location.Start - part.StartAt;
            int length = location.End - location.Start;
            if (length + offsetStart > part.Body.Length)
            {
                length = part.Body.Length - offsetStart;
            }
            return (part, offsetStart, length);
        }

        internal (string query, string filename) ResolveQuery(GraphQLLocation location)
        {
            (LocatedNamedSource part, int offsetStart, int length) = ResolveNode(location);

            string text = part.Body.Substring(offsetStart, length);

            return (text, part.Path);
        }

        internal IGraphQLType ResolveType(GraphQLParser.AST.OperationType type)
        {
            var schema = ast.Definitions.OfType<GraphQLSchemaDefinition>().FirstOrDefault();
            if (schema != null)
            {
                var namedType = schema.OperationTypes.FirstOrDefault(x => x.Operation == type)?.Type;
                if (namedType != null)
                {
                    return ResolveType(namedType);
                }
            }
            return null;
        }

        internal IGraphQLType ResolveType(GraphQLNamedType type)
        {
            return ResolveType(type.Name.Value);
        }

        internal IGraphQLType ResolveType(string typeName)
        {
            IGraphQLType result = this.types.SingleOrDefault(x => x.Name == typeName);

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
                this.types.Add(result);

            }

            return result;
        }


        private void UnPackType(GraphQLType type, ValueTypeReference target)
        {
            try
            {
                if (type is GraphQLNonNullType nonNullType)
                {
                    target.CanValueBeNull = false;
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
            }
            catch (Exception ex)
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
        public bool CanValueBeNull { get; set; } = true;
        public bool IsCollection { get; set; } = false;
        public bool CanCollectionBeNull { get; set; } = true;
        public IGraphQLType Type { get; set; }

        public override string ToString()
        {
            string val = this.Type.Name;
            if (!this.CanValueBeNull)
            {
                val += "!";
            }
            if (this.IsCollection)
            {
                val = "[" + val + "]";
            }
            if (this.CanCollectionBeNull)
            {
                val += "!";
            }

            return val;
        }
    }
}
