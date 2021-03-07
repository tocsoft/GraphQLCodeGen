using HotChocolate.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tocsoft.GraphQLCodeGen
{
    public class AstPrinter
    {
        private readonly IEnumerable<string> directivesToRemove;

        public AstPrinter() : this(Enumerable.Empty<string>()) { }
        public AstPrinter(string directivetoSkip) : this(directivetoSkip == null ? Enumerable.Empty<string>() : new[] { directivetoSkip }) { }
        public AstPrinter(IEnumerable<string> directivesToRemove)
        {
            this.directivesToRemove = directivesToRemove;
        }

        public string Print(ISyntaxNode node)
        {
            var rewittenNode = new DirectiveRemover(directivesToRemove).Rewrite(node, null);
            return rewittenNode.ToString(true);
        }


        private class DirectiveRemover : QuerySyntaxRewriter<object>
        {
            private readonly IEnumerable<string> directivesToRemove;

            public DirectiveRemover(IEnumerable<string> directivesToRemove)
            {
                this.directivesToRemove = directivesToRemove;
            }
            protected override TParent RewriteDirectives<TParent>(TParent parent, IReadOnlyList<DirectiveNode> directives, object context, Func<IReadOnlyList<DirectiveNode>, TParent> rewrite)
            {
                return base.RewriteDirectives(parent, directives, context, rewrite);
            }
            protected override DirectiveNode RewriteDirective(DirectiveNode node, object context)
            {
                return base.RewriteDirective(node, context);
            }
            
            protected override FieldNode RewriteField(FieldNode node, object context)
            {
                if (node.Directives.Any())
                {
                    var directives = node.Directives.Where(x => !directivesToRemove.Contains(x.Name.Value)).ToList();
                    node = node.WithDirectives(directives);
                }

                return base.RewriteField(node, context);
            }

            protected override ObjectFieldNode RewriteObjectField(ObjectFieldNode node, object context)
            {
                return base.RewriteObjectField(node, context);
            }
        }
    }
}
