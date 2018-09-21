using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Tocsoft.GraphQLCodeGen.CodeGeneratorSettingsLoader;

namespace Tocsoft.GraphQLCodeGen.SchemaIntrospection
{
    public class JsonIntrospection : IIntrosepctionProvider
    {
        public SchemaSource.SchemaTypes SchemaType => SchemaSource.SchemaTypes.Json;

        static Dictionary<string, string> parseCache = new Dictionary<string, string>();

        public Task<string> LoadSchema(SchemaSource source)
        {
            if (!parseCache.TryGetValue(source.Location, out var schema))
            {
                schema = ConvertJsonToSchema(File.ReadAllText(source.Location));
                parseCache.Add(source.Location, schema);
            }
            return Task.FromResult(schema);
        }

        public static string ConvertJsonToSchema(string json)
        {
            wrapper.Rootobject model = Newtonsoft.Json.JsonConvert.DeserializeObject<wrapper.Rootobject>(json);
            wrapper.__Schema scheme = model.__schema ?? model.data.__schema;
            StringBuilder sb = new StringBuilder();
            scheme.Generate(sb);
            return sb.ToString();
        }
        private class wrapper
        {
            public class Rootobject
            {
                public Data data { get; set; }
                public __Schema __schema { get; set; }
            }

            public class Data
            {
                public __Schema __schema { get; set; }
            }

            public class __Schema
            {
                public NamedType queryType { get; set; }
                public NamedType mutationType { get; set; }
                public NamedType subscriptionType { get; set; }
                public SchemeType[] types { get; set; }
                public Directive[] directives { get; set; }

                public void Generate(StringBuilder sb)
                {
                    sb.AppendLine("schema {");
                    if (queryType != null && queryType.name != null)
                    {
                        sb.AppendLine($"query: {queryType.name}");
                    }
                    if (mutationType != null && mutationType.name != null)
                    {
                        sb.AppendLine($"mutation: {mutationType.name}");
                    }
                    sb.AppendLine("}");
                    sb.AppendLine();

                    foreach (SchemeType t in this.types.Reverse())
                    {
                        if (t.Generate(sb))
                        {
                            sb.AppendLine();
                            sb.AppendLine();
                        }
                    }
                }
            }

            public class NamedType
            {
                public string name { get; set; }
            }

            public class SchemeType
            {
                public string kind { get; set; }
                public string name { get; set; }
                public string description { get; set; }
                public Field[] fields { get; set; }
                public Inputfield[] inputFields { get; set; }
                public TypePointer[] interfaces { get; set; }
                public Enumvalue[] enumValues { get; set; }
                public TypePointer[] possibleTypes { get; set; }

                public bool Generate(StringBuilder sb)
                {
                    if (this.name.StartsWith("__")) { return false; } // seems to be an intrinsic.

                    if (this.kind == "OBJECT" || this.kind == "INTERFACE" || this.kind == "INPUT_OBJECT")
                    {
                        switch (this.kind)
                        {
                            case "OBJECT":
                                sb.Append("type ");
                                break;
                            case "INTERFACE":
                                sb.Append("interface ");
                                break;
                            case "INPUT_OBJECT":
                                sb.Append("input ");
                                break;
                            default:
                                break;
                        }
                        sb.Append(this.name);

                        if (this.interfaces != null && this.interfaces.Any())
                        {
                            sb.Append(" implements ");
                            sb.Append(this.interfaces.First().name);
                            foreach (TypePointer i in this.interfaces.Skip(1))
                            {
                                sb.Append(", ");
                                sb.Append(i.name);
                            }
                        }

                        sb.AppendLine("{");
                        if (this.fields != null)
                        {
                            foreach (Field f in this.fields)
                            {
                                f.Generate(sb);
                            }
                        }
                        if (this.inputFields != null)
                        {
                            foreach (Inputfield f in this.inputFields)
                            {
                                f.Generate(sb);
                            }
                        }

                        sb.Append("}");
                        return true;
                    }
                    else if (this.kind == "UNION")
                    {

                        sb.Append("union ");
                        sb.Append(this.name);
                        sb.Append(" = ");

                        sb.Append(this.possibleTypes.First().name);
                        foreach (TypePointer i in this.possibleTypes.Skip(1))
                        {
                            sb.Append(" | ");
                            sb.Append(i.name);
                        }
                        return true;
                    }
                    else if (this.kind == "ENUM")
                    {

                        sb.Append("enum ");
                        sb.Append(this.name);
                        sb.AppendLine(" {");
                        foreach (Enumvalue i in this.enumValues)
                        {
                            sb.AppendLine(i.name);
                        }
                        sb.AppendLine("}");
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            public class Field
            {
                public string name { get; set; }
                public string description { get; set; }
                public Arg[] args { get; set; }
                public TypePointer type { get; set; }
                public bool isDeprecated { get; set; }
                public string deprecationReason { get; set; }

                public void Generate(StringBuilder sb)
                {
                    sb.Append(this.name);

                    if (this.args != null && this.args.Any())
                    {
                        sb.Append('(');
                        this.args.First().Generate(sb);
                        foreach (Arg a in this.args.Skip(1))
                        {
                            sb.Append(", ");
                            a.Generate(sb);
                        }

                        sb.Append(')');
                    }

                    sb.Append(": ");
                    this.type.Generate(sb);
                    sb.AppendLine();
                }
            }

            public class TypePointer
            {
                public string kind { get; set; }
                public string name { get; set; }
                public TypePointer ofType { get; set; }

                public void Generate(StringBuilder sb)
                {
                    if (this.kind == "NON_NULL")
                    {
                        // then add '!' to end of child type 
                        this.ofType.Generate(sb);
                        sb.Append('!');
                    }
                    if (this.kind == "LIST")
                    {
                        // //wrapped in '[' & ']'
                        sb.Append('[');
                        this.ofType.Generate(sb);
                        sb.Append(']');
                    }
                    else
                    {
                        sb.Append(this.name);
                    }
                }
            }

            public class Arg
            {
                public string name { get; set; }
                public string description { get; set; }
                public TypePointer type { get; set; }
                public string defaultValue { get; set; }

                internal void Generate(StringBuilder sb)
                {
                    sb.Append(this.name);
                    sb.Append(": ");
                    this.type.Generate(sb);

                    if (!string.IsNullOrWhiteSpace(this.defaultValue))
                    {
                        sb.Append(" = ");
                        sb.Append(this.defaultValue);
                    }
                }
            }

            public class Inputfield
            {
                public string name { get; set; }
                public string description { get; set; }
                public TypePointer type { get; set; }

                public void Generate(StringBuilder sb)
                {
                    sb.Append(this.name);
                    sb.Append(": ");
                    this.type.Generate(sb);
                    sb.AppendLine();
                }
            }


            public class Enumvalue
            {
                public string name { get; set; }
                public string description { get; set; }
                public bool isDeprecated { get; set; }
                public object deprecationReason { get; set; }
            }

            public class Directive
            {
                public string name { get; set; }
                public string description { get; set; }
                public string[] locations { get; set; }
                public Arg[] args { get; set; }
            }
        }
    }
}
