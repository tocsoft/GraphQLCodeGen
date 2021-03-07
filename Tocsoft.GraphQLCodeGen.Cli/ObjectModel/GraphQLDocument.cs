using Tocsoft.GraphQLCodeGen.ObjectModel.Selections;
using HotChocolate.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Tocsoft.GraphQLCodeGen.IntrospectedSchemeParser;

namespace Tocsoft.GraphQLCodeGen.ObjectModel
{
    internal enum ErrorCodes
    {
        Unknown = 0,
        UnhandledException = 1,
        UnknownField = 2,
        SyntaxError = 3,
        TemplateError = 4,
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
        public GraphQLDocument(DocumentNode ast, IEnumerable<LocatedNamedSource> queryParts, CodeGeneratorSettings settings)
        {
            this.ast = ast;

            this.QueryParts = queryParts;
            this.settings = settings;
            List<object> items = ast.Definitions.Select(this.Visit).ToList();
            this.Operations = items.OfType<Operation>().ToList();
            this.types = items.OfType<IGraphQLType>().GroupBy(x=>x.Name).Select(x=>x.First()).ToList();
            this.astPrinter = new AstPrinter(settings.TypeNameDirective);
            foreach (IGraphQLInitter i in items.OfType<IGraphQLInitter>().Where(x => !(x is Operation)))
            {
                i.Resolve(this);
            }
            foreach (Operation i in items.OfType<Operation>())
            {
                i.Resolve(this);
            }
        }

        internal IValueNode<string> ResolveSpecifiedTypeName(IEnumerable<DirectiveNode> directives)
        {
            var directive = directives.SingleOrDefault(x => x.Name.Value == this.settings.TypeNameDirective);
            var value = directive?.Arguments.SingleOrDefault(x => x.Name.Value == "type")?.Value;
            var scalar = value as IValueNode<string>;
            return scalar;
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

        internal void AddError(ErrorCodes code, string message, ISyntaxNode node)
            => AddError(code, message, node.Location);

        internal void AddError(ErrorCodes code, string message, Location location)
        {
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

        private readonly DocumentNode ast;
        private readonly CodeGeneratorSettings settings;

        public IEnumerable<LocatedNamedSource> QueryParts { get; private set; }

        private List<IGraphQLType> types;
        private readonly AstPrinter astPrinter;

        internal ValueTypeReference ResolveValueType(ITypeNode type)
        {
            ValueTypeReference result = new ValueTypeReference();
            UnPackType(type, result);

            return result;
        }

        internal (LocatedNamedSource part, int offset, int length) ResolveNode(Location location)
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

        internal string ResolveQuerySource(Location location)
        {
            (LocatedNamedSource part, _, _) = ResolveNode(location);

            return part.Path;
        }

        internal string ResolveQuery(OperationDefinitionNode operation)
        {
            return astPrinter.Print(operation);
        }
        internal string ResolveFragment(FragmentDefinitionNode operation)
        {
            return astPrinter.Print(operation);
        }

        internal IGraphQLType ResolveType(OperationType type)
        {
            var schema = ast.Definitions.OfType<SchemaDefinitionNode>().FirstOrDefault();
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

        internal IGraphQLType ResolveType(NamedTypeNode type)
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


        private void UnPackType(ITypeNode type, ValueTypeReference target)
        {
            try
            {
                if (type is NonNullTypeNode nonNullType)
                {
                    target.CanValueBeNull = false;
                    UnPackType(nonNullType.Type, target);
                }
                else if (type is ListTypeNode listType)
                {
                    target.IsCollection = true;
                    if (target.CanValueBeNull)
                    {
                        target.CanValueBeNull = false;
                        target.CanCollectionBeNull = true;
                    }
                    UnPackType(listType.Type, target);
                }
                else if (type is NamedTypeNode namedType)
                {
                    target.Type = ResolveType(namedType);
                }
                else
                {
                    throw new Exception("dunno???");
                }
            }
            catch
            {
                throw;
            }
        }
      
        private object Visit(ISyntaxNode node)
        {
            switch (node)
            {
                
                case OperationDefinitionNode op:
                    return new Operation(op);
                case InterfaceTypeDefinitionNode op:
                    return new InterfaceType(op);
                case ObjectTypeDefinitionNode op:
                    return new ObjectType(op);
                case EnumTypeDefinitionNode op:
                    return new EnumType(op);
                case UnionTypeDefinitionNode op:
                    return new UnionType(op);
                case InputObjectTypeDefinitionNode op:
                    return new ObjectType(op);
                case FragmentDefinitionNode op:
                    return new FragmentType(op);
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
