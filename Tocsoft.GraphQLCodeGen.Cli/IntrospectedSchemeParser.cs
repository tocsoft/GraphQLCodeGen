using Tocsoft.GraphQLCodeGen.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Tocsoft.GraphQLCodeGen
{
    internal class IntrospectedSchemeParser
    {
        public static GraphQLDocument Parse(IEnumerable<NamedSource> parts, CodeGeneratorSettings settings)
        {
            List<LocatedNamedSource> sources = new List<LocatedNamedSource>();
            try
            {
                StringBuilder sb = new StringBuilder();
                int linecount = 0;

                var codeGenDirective = $"directive @{settings.TypeNameDirective}( type: String ) on FIELD_DEFINITION";
                linecount += codeGenDirective.Count(x => x == '\n') + 1;
                sb.Append(codeGenDirective);
                sb.Append('\n');

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

                var bytes = Encoding.UTF8.GetBytes(final);
                var hcparser = new HotChocolate.Language.Utf8GraphQLParser(bytes);
                var doc = hcparser.Parse();

                //var parser = new Parser(new Lexer());
                //var parsedDocument = parser.Parse(new Source(final));

                return new GraphQLDocument(doc, sources, settings);
            }
            catch (HotChocolate.Language.SyntaxException sex)
            {
                var message = sex.Message;

                int col = sex.Column;
                int line = sex.Line;

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
            catch (Exception fex)
            {
                var t = fex;
                return GraphQLDocument.Error(ErrorCodes.UnhandledException, fex.ToString());
            }
        }

        public class LocatedNamedSource
        {
            public int StartAt { get; set; }
            public int EndAt { get; set; }
            public string Path { get; set; }
            public string Body { get; set; }
            public int LineStartAt { get; internal set; }

            private string[] lines;

            public string Line(int line)
            {
                line = line - 1;
                if (lines == null)
                {

                    lines = GetLines(Body).ToArray();
                }

                if (line < 0)
                {
                    return null;
                }

                if (line >= lines.Length)
                {

                    return null;
                }

                return lines[line];
            }

            private static IEnumerable<string> GetLines(string str, bool removeEmptyLines = false)
            {
                using (var sr = new StringReader(str))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (removeEmptyLines && String.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }
                        yield return line;
                    }
                }
            }
        }
    }

    public class NamedSource
    {
        public string Path { get; set; }
        public string Body { get; set; }
    }
}
