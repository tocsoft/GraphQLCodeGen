using HotChocolate.Language;
using System.Collections;
using System.Collections.Generic;

namespace Tocsoft.GraphQLCodeGen.ObjectModel
{
    internal interface IGraphQLFieldCollection : IGraphQLType
    {
        IEnumerable<Field> Fields { get; }
    }

    internal interface IGraphQLType
    {
        string Name { get; }
    }
    internal interface IGraphQLInitter
    {
        void Resolve(GraphQLDocument doc);
    }
    internal interface IGraphQLASTNodeLinked
    {
        ISyntaxNode ASTNode { get; }
    }
}