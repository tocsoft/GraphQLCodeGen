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
        public static GraphQLDocument Parse(IEnumerable<NamedSource> parts)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                List<LocatedNamedSource> sources = new List<LocatedNamedSource>();
                foreach (NamedSource part in parts)
                {
                    int start = sb.Length;
                    string body = part.Body.Replace("\r", "");
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

                string final = sb.ToString();
                return new GraphQLDocument(new Parser(new Lexer()).Parse(new Source(final)), sources);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public class LocatedNamedSource
        {
            public int StartAt { get; set; }
            public int EndAt { get; set; }
            public string Path { get; set; }
            public string Body { get; set; }
        }

    }

    public class NamedSource
    {
        public string Path { get; set; }
        public string Body { get; set; }
    }
}
