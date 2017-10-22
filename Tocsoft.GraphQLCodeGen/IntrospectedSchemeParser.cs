using Tocsoft.GraphQLCodeGen.ObjectModel;
using GraphQLParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tocsoft.GraphQLCodeGen
{
    internal class IntrospectedSchemeParser
    {
        public static string JsonToTypeDefinition(string json)
        {
            var model = Newtonsoft.Json.JsonConvert.DeserializeObject<wrapper.Rootobject>(json);
            var scheme = model.__schema ?? model.data.__schema;
            var sb = new StringBuilder();
            scheme.Generate(sb);
            return sb.ToString();
        }

        public static GraphQLDocument Parse(IEnumerable<NamedSource> parts)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                List<LocatedNamedSource> sources = new List<LocatedNamedSource>();
                foreach (var part in parts)
                {
                    int start = sb.Length;
                    var body = part.Body.Replace("\r", "");
                    sb.Append(body);
                    sb.Append("\n");
                    sources.Add(new LocatedNamedSource()
                    {
                        StartAt = start,
                        EndAt = sb.Length,
                        Path = part.Path,
                        Body = body
                    });
                }

                var final = sb.ToString();
                return new GraphQLDocument(new Parser(new Lexer()).Parse(new Source(final)), sources);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public class LocatedNamedSource
        {
            public int StartAt { get; set; }
            public int EndAt { get; set; }
            public string Path { get; set; }
            public string Body { get; set; }
        }

        public class NamedSource
        {
            public string Path { get; set; }
            public string Body { get; set; } 
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
