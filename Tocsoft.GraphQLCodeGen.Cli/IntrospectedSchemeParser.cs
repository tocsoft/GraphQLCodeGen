using Tocsoft.GraphQLCodeGen.ObjectModel;
using GraphQLParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GraphQLParser.Exceptions;

namespace Tocsoft.GraphQLCodeGen
{
    internal class IntrospectedSchemeParser
    {
        public static GraphQLDocument Parse(IEnumerable<NamedSource> parts)
        {
            List<LocatedNamedSource> sources = new List<LocatedNamedSource>();
            try
            {
                StringBuilder sb = new StringBuilder();
                int linecount = 0;
                foreach (NamedSource part in parts)
                {
                    int start = sb.Length;
                    string body = part.Body.Replace("\r", "");

                    sb.Append(body);
                    sb.Append("\n");
                    sources.Add(new LocatedNamedSource()
                    {
                        LineStartAt = linecount,
                        StartAt = start,
                        EndAt = sb.Length,
                        Path = part.Path,
                        Body = body
                    });

                    linecount += body.Count(x => x == '\n') + 1;
                }

                string final = sb.ToString();
                var parser = new Parser(new Lexer());
                var parsedDocument = parser.Parse(new Source(final));

                return new GraphQLDocument(parsedDocument, sources);
            }
            catch (GraphQLSyntaxErrorException ex)
            {
                var msg = ex.ToString();
                var trimedStart = msg.Substring("GraphQLParser.Exceptions.GraphQLSyntaxErrorException: Syntax Error GraphQL (".Length);
                var lineColSpliiter = trimedStart.IndexOf(':');
                var lineColEnder = trimedStart.IndexOf(')');
                int line = int.Parse(trimedStart.Substring(0, lineColSpliiter));
                int col = int.Parse(trimedStart.Substring(lineColSpliiter+1, lineColEnder - lineColSpliiter -1));

                var endOfLine = trimedStart.IndexOf('\n');
                var message = trimedStart.Substring(lineColEnder + 1, endOfLine - lineColEnder - 1).Trim();

                var source = sources.Where(x => x.LineStartAt <= line).OrderByDescending(x => x.LineStartAt).FirstOrDefault();
                return GraphQLDocument.Error(new GraphQLError
                {
                    Path = source?.Path,
                    Line = source == null ? (int?)null : line - source.LineStartAt,
                    Column = col,
                    Code = ErrorCodes.SyntaxError,
                    Message = message
                });
            }
            catch (Exception ex)
            {
                return GraphQLDocument.Error(ErrorCodes.UnhandledException, ex.ToString());
            }
        }
        public class LocatedNamedSource
        {
            public int StartAt { get; set; }
            public int EndAt { get; set; }
            public string Path { get; set; }
            public string Body { get; set; }
            public int LineStartAt { get; internal set; }
        }

    }

    public class NamedSource
    {
        public string Path { get; set; }
        public string Body { get; set; }
    }
}
