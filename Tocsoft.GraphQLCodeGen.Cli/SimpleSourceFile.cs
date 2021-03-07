using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Tocsoft.GraphQLCodeGen
{
    [DebuggerDisplay("{Path}")]
    internal class SimpleSourceFile
    {
        public string Path { get; set; }
        public string Body { get; set; }
        public string Format { get; set; }
        public string Flavor { get; set; }
        public string ClassName { get; set; }
        public string TypeNameDirective { get; set; }
        public string OutputPath { get; set; }
        public List<string> Templates { get; set; } = new List<string>();
        public Dictionary<string, string> TemplateSettings { get; set; } = new Dictionary<string, string>();
        public List<string> Includes { get; set; } = new List<string>();
        public SchemaSource SchemaSource { get; set; }
        public string RootPath { get; set; }

        internal string SettingsHash()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(ClassName);
            sb.Append("~#~");
            sb.Append(OutputPath);
            sb.Append("~#~");
            sb.Append(RootPath);
            sb.Append("~#~");
            sb.Append(Format);
            sb.Append("~#~");
            sb.Append(TypeNameDirective);
            sb.Append("~#~");
            if (Templates != null)
            {
                foreach (var t in Templates)
                {
                    sb.Append(t);
                    sb.Append("~#~");
                }
            }
            if (TemplateSettings != null)
            {
                foreach (var t in TemplateSettings)
                {
                    sb.Append(t.Key);
                    sb.Append("~#~");
                    sb.Append(t.Value);
                    sb.Append("~#~");
                }
            }

            if (SchemaSource != null)
            {
                sb.Append(SchemaSource.SettingsHash());
            }

            return sb.ToString();
        }
    }
}
