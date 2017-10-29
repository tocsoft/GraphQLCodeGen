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


        public Task<string> LoadSchema(SchemaSource source)
        {
            var schema = ConvertJsonToSchema(File.ReadAllText(source.Location));
            return Task.FromResult(schema);
        }

        public static string ConvertJsonToSchema(string json)
        {
            var model = Newtonsoft.Json.JsonConvert.DeserializeObject<wrapper.Rootobject>(json);
            var scheme = model.__schema ?? model.data.__schema;
            var sb = new StringBuilder();
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
                    foreach (var t in types.Reverse())
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
                    if (name.StartsWith("__")) { return false; } // seems to be an intrinsic.

                    if (kind == "OBJECT" || kind == "INTERFACE" || kind == "INPUT_OBJECT")
                    {
                        switch (kind)
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
                        sb.Append(name);

                        if (interfaces != null && interfaces.Any())
                        {
                            sb.Append(" implements ");
                            sb.Append(interfaces.First().name);
                            foreach (var i in interfaces.Skip(1))
                            {
                                sb.Append(", ");
                                sb.Append(i.name);
                            }
                        }

                        sb.AppendLine("{");
                        if (this.fields != null)
                        {
                            foreach (var f in this.fields)
                            {
                                f.Generate(sb);
                            }
                        }
                        if (this.inputFields != null)
                        {
                            foreach (var f in this.inputFields)
                            {
                                f.Generate(sb);
                            }
                        }

                        sb.Append("}");
                        return true;
                    }
                    else if (kind == "UNION")
                    {

                        sb.Append("union ");
                        sb.Append(name);
                        sb.Append(" = ");

                        sb.Append(possibleTypes.First().name);
                        foreach (var i in possibleTypes.Skip(1))
                        {
                            sb.Append(" | ");
                            sb.Append(i.name);
                        }
                        return true;
                    }
                    else if (kind == "ENUM")
                    {

                        sb.Append("enum ");
                        sb.Append(name);
                        sb.AppendLine(" {");
                        foreach (var i in this.enumValues)
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
                    sb.Append(name);

                    if (args != null && args.Any())
                    {
                        sb.Append('(');
                        args.First().Generate(sb);
                        foreach (var a in args.Skip(1))
                        {
                            sb.Append(", ");
                            a.Generate(sb);
                        }

                        sb.Append(')');
                    }

                    sb.Append(": ");
                    type.Generate(sb);
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
                    if (kind == "NON_NULL")
                    {
                        // then add '!' to end of child type 
                        ofType.Generate(sb);
                        sb.Append('!');
                    }
                    if (kind == "LIST")
                    {
                        // //wrapped in '[' & ']'
                        sb.Append('[');
                        ofType.Generate(sb);
                        sb.Append(']');
                    }
                    else
                    {
                        sb.Append(name);
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
                    sb.Append(name);
                    sb.Append(": ");
                    type.Generate(sb);

                    if (!string.IsNullOrWhiteSpace(defaultValue))
                    {
                        sb.Append(" = ");
                        sb.Append(defaultValue);
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
                    sb.Append(name);
                    sb.Append(": ");
                    type.Generate(sb);
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
