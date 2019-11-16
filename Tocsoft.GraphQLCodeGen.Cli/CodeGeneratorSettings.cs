using System.Collections.Generic;

namespace Tocsoft.GraphQLCodeGen
{
    public class CodeGeneratorSettings
    {
        public string Namespace { get; set; }
        public string ClassName { get; set; }
        public string OutputPath { get; set; }
        public string Format { get; set; }
        public string TypeNameDirective { get; set; }
        public IEnumerable<NamedSource> SourceFiles { get; set; }

        public IEnumerable<string> Templates { get; set; }
        public IDictionary<string, string> TemplateSettings { get; internal set; }
        internal SchemaSource Schema { get; set; }
    }
}
