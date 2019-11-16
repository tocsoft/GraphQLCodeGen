using GlobExpressions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Tocsoft.GraphQLCodeGen
{
    public static class GlobExpander
    {
        public static IEnumerable<string> FindFiles(string pattern)
        {
            string root = Directory.GetCurrentDirectory();
            return FindFiles(root, pattern);
        }
        public static IEnumerable<string> FindFiles(string root, IEnumerable<string> patterns)
        {
            return FindFilesInternal(root, patterns).ToList();
        }

        public static IEnumerable<string> FindFiles(IEnumerable<string> patterns)
        {
            string root = Directory.GetCurrentDirectory();
            return FindFiles(root, patterns);
        }

        public static IEnumerable<string> FindFiles(string root, string pattern)
        {
            return FindFilesInternal(root, pattern).ToList();
        }

        public static string FindFile(string root, string pattern)
        {
            return FindFilesInternal(root, pattern).FirstOrDefault();
        }

        public static string FindFile(string pattern)
        {
            string root = Directory.GetCurrentDirectory();
            return FindFile(root, pattern);
        }

        private static IEnumerable<string> FindFilesInternal(string root, IEnumerable<string> pattern)
        {
            return pattern.SelectMany(p => FindFilesInternal(root, p)).Distinct();
        }
        private static IEnumerable<string> FindFilesInternal(string root, string pattern)
        {
            if (pattern.StartsWith(".\\") || pattern.StartsWith("./"))
            {
                pattern = pattern.Substring(2);
            }
            string prefix = "";
            char[] toFind = new[] { '*', '[', '{' };
            var matches = pattern.Select((x, i) => new { x, i, isMatch = toFind.Contains(x) });

            if (!matches.Any(x => x.isMatch))
            {
                // we don't actually match any parts which means we are actuaslly only a single file
                yield return GetPath(root, pattern);
            }
            else
            {
                var match = pattern.Select((x, i) => new { x, i, isMatch = toFind.Contains(x) })
                    .FirstOrDefault(x => x.isMatch);

                prefix = ((string)pattern).Substring(0, match.i);
                pattern = ((string)pattern).Substring(match.i);
                root = GetPath(root, prefix);

                if (Path.IsPathRooted(pattern))
                {
                    yield return Path.GetFullPath(pattern);
                }
                else
                {
                    string rootPath = Path.GetFullPath(root);
                    Glob glob = new Glob(pattern);
                    IEnumerable<string> files = Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories);

                    foreach (string f in files)
                    {
                        string sub = f.Substring(rootPath.Length);

                        if (glob.IsMatch(sub))
                        {
                            yield return f;
                        }
                    }
                }

            }
        }

        private static string GetPath(string root, string path)
        {
            if (Path.IsPathRooted(path))
            {
                return Path.GetFullPath(path);
            }

            return Path.GetFullPath(Path.Combine(root, path));
        }
    }
}
