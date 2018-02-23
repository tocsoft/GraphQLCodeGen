using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Tocsoft.GraphQLCodeGen.RelectionHelpers
{
    internal class GraphQLConventionsRequestHandlerBuilder
    {
        private readonly object builder;
        private readonly MethodInfo addQueryMethod;
        private readonly MethodInfo addMutationMethod;
        private readonly MethodInfo generateMethod;
        private readonly MethodInfo describeSchema;

        public GraphQLConventionsRequestHandlerBuilder( Assembly conventionsAssembly)
        {
            var allTypes = conventionsAssembly.AllLoadableTypes();
            var builderType = allTypes.SingleOrDefault(x=>x.FullName == "GraphQL.Conventions.Web.RequestHandler+RequestHandlerBuilder");
            var rootHandler = allTypes.SingleOrDefault(x => x.FullName == "GraphQL.Conventions.Web.RequestHandler");
            var newBuidlerMethod = rootHandler.GetMethod("New");

            this.builder = newBuidlerMethod.Invoke(null, new object[0]);


            addQueryMethod = builderType.GetMethod("WithQuery", new Type[] { typeof(Type) });
            addMutationMethod = builderType.GetMethod("WithMutation", new Type[] { typeof(Type) });
            generateMethod = builderType.GetMethod("Generate", new Type[] { });


            this.describeSchema = conventionsAssembly.GetType("GraphQL.Conventions.Web.IRequestHandler")
                                        .GetMethod("DescribeSchema",new[] { typeof(bool), typeof(bool), typeof(bool) });

        }

        private IEnumerable<Type> FindTypes(IEnumerable<string> patterns, IEnumerable<Assembly> source)
        {
            var allTypes = source.AllLoadableTypes();
            foreach (var pattern in patterns)
            {
                var cleanedPattern = pattern.Trim();
                if (cleanedPattern.StartsWith("[") && cleanedPattern.EndsWith("]"))
                {
                    cleanedPattern = cleanedPattern.Trim('[', ']');
                    var cleanedPatternAlt = cleanedPattern + "Attribute";

                    var matches = allTypes.Where(x => !x.GetTypeInfo().IsAbstract && x.GetTypeInfo().GetCustomAttributes().Any(a =>
                    {
                        var n = a.GetType().Name;
                        var fn = a.GetType().FullName;
                        return n.Equals(cleanedPattern, StringComparison.OrdinalIgnoreCase) ||
                        n.Equals(cleanedPatternAlt, StringComparison.OrdinalIgnoreCase) ||
                        fn.Equals(cleanedPatternAlt, StringComparison.OrdinalIgnoreCase) ||
                        fn.Equals(cleanedPatternAlt, StringComparison.OrdinalIgnoreCase);
                    }));

                    foreach (var m in matches)
                    {
                        yield return m;
                    }
                    // we are using attribute matching
                }
                else
                {
                    // we are using typename matching
                    var type = allTypes.FirstOrDefault(t => t.FullName.Equals(cleanedPattern, StringComparison.OrdinalIgnoreCase));
                    if (type != null)
                    {
                        yield return type;
                    }
                }
            }
        }

        public void AddQueries(IEnumerable<string> patterns, IEnumerable<Assembly> source)
        {
            var types = FindTypes(patterns, source).ToList();

            foreach (var t in types)
            {
                addQueryMethod.Invoke(this.builder, new[] { t });
            }
        }
        public void AddMutations(IEnumerable<string> patterns, IEnumerable<Assembly> source)
        {
            var types = FindTypes(patterns, source);

            foreach (var t in types)
            {
                addMutationMethod.Invoke(this.builder, new[] { t });
            }
        }
        public string GenerateSchema()
        {
            var handler = generateMethod.Invoke(this.builder,null);

            return (string)this.describeSchema.Invoke(handler, new object[] { false, true, true });
        }
    }

    internal static class AssembliExtensions
    {
        public static IEnumerable<Type> AllLoadableTypes(this IEnumerable<Assembly> assemblies)
        {
            return assemblies.SelectMany(x => x.AllLoadableTypes()).ToList();
        }
        public static IEnumerable<Type> AllLoadableTypes(this Assembly assembly)
        {
            try
            {
                return assembly.GetTypes().Where(x => x != null);
            }catch(ReflectionTypeLoadException ex)
            {
                return ex.Types?.Where(x=>x != null) ?? Enumerable.Empty<Type>();
            }
        }
    }
}
