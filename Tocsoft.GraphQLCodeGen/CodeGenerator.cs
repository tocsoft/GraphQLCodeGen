using GraphQLParser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Tocsoft.GraphQLCodeGen.IntrospectedSchemeParser;

namespace Tocsoft.GraphQLCodeGen
{
    public class CodeGenerator
    {
        private readonly CodeGeneratorSettings settings;

        public CodeGenerator(CodeGeneratorSettings settings)
        {
            this.settings = settings;
        }

        public void Generate()
        {
            // we need to load in the scheme
            var schema = JsonToTypeDefinition(File.ReadAllText(settings.SchemaPath));
            var sources = new List<NamedSource>();
            sources.Add(new NamedSource() { Path = settings.SchemaPath, Body = schema });

            foreach (var s in settings.SourcePaths)
            {
                sources.Add(new NamedSource() { Path = s, Body = File.ReadAllText(s) });
            }
            // we want to track the file that the operation is loaded from
            // lets make a locatino index look up table and provide it
            var doc = Parse(sources);

            var model = new Models.ViewModel(doc, settings);
            var templateName = settings.Template;
            if (settings.Template == null)
            {
                // lets guess at a template based on file extension
            }

            var fileResult = new TemplateEngine(templateName).Generate(model);

            File.WriteAllText(settings.OutputPath, fileResult);
        }

    }

    public class CodeGeneratorSettings
    {
        public string SettingsPath { get; set; }
        public string Namespace { get; set; }
        public string ClassName { get; set; }
        public string OutputPath { get; set; }
        public IEnumerable<string> SourcePaths { get; set; }
        public string SchemaPath { get; set; }
        public string Template { get; set; }
        public string SourcePath { get; set; }
    }

    public class CodeGeneratorSettingsLoader
    {
        public CodeGeneratorSettingsLoader()
        {

        }

        public IEnumerable<CodeGeneratorSettings> LoadFromPath(string path)
        {
            var paths = GetPaths(Directory.GetCurrentDirectory(), path);

            return paths.Select(LoadSingleFromPath).ToList();
        }

        public CodeGeneratorSettings LoadSingleFromPath(string path)
        {
            // rootPath
            var dir = Path.GetDirectoryName(path);

            var json = File.ReadAllText(path);
            var simple = Newtonsoft.Json.JsonConvert.DeserializeObject<SimpleSettings>(json);

            var settings = new CodeGeneratorSettings();
            settings.SchemaPath = GetPath(dir, simple.Schema);
            settings.OutputPath = GetPath(dir, simple.Output);
            settings.SourcePaths = GetPaths(dir, simple.Source);
            settings.ClassName = simple.Classname;
            settings.Namespace = simple.Namespace;
            settings.Template = simple.Template;
            settings.SettingsPath = path;
            return settings;
        }

        private IEnumerable<string> GetPaths(string root, string path)
        {
            var prefix = "";
            var toFind = new[] { '*', '[', '{' };
            var matches = path.Select((x, i) => (x, i, isMatch: toFind.Contains(x)));

            if (matches.Any())
            {
                var match = path.Select((x, i) => (x, i, isMatch: toFind.Contains(x)))
                    .FirstOrDefault(x => x.isMatch);

                prefix = path.Substring(0, match.i);
                path = path.Substring(match.i);
                root = GetPath(root, prefix);
            }

            if (Path.IsPathRooted(path))
            {
                return new[] { Path.GetFullPath(path) };
            }
            else
            {
                var rootPAth = Path.GetFullPath(root);
                var glob = new Glob.Glob(path);
                var files = Directory.GetFiles(rootPAth, "*.*", SearchOption.AllDirectories);
                var filesMapped = files.Select(x => new
                {
                    p = x,
                    sub = x.Substring(rootPAth.Length)
                });
                var filter = filesMapped.Where(x => glob.IsMatch(x.sub));
                return filter.Select(x => x.p).ToList();
            }
        }
        private string GetPath(string root, string path)
        {
            if (Path.IsPathRooted(path))
            {
                return Path.GetFullPath(path);
            }

            return Path.GetFullPath(Path.Combine(root, path));
        }


        public class SimpleSettings
        {
            public string Namespace { get; set; }
            public string Classname { get; set; }
            public string Output { get; set; }
            public string Source { get; set; }
            public string Schema { get; set; }
            public string Template { get; set; }
        }

    }
}
