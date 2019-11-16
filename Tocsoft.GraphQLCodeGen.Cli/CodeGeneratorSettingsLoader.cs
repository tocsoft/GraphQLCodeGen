using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Tocsoft.GraphQLCodeGen.Cli;

namespace Tocsoft.GraphQLCodeGen
{
    internal class CodeGeneratorSettingsLoader
    {
        public CodeGeneratorSettingsLoader(ILogger logger)
        {
            this.logger = logger;
        }

        private List<string> DefaultTemplates(string outputPath)
        {
            string type = Path.GetExtension(outputPath).Trim('.').ToLower();
            TypeInfo info = typeof(CodeGeneratorSettings).GetTypeInfo();
            string templateSet = info.Namespace + ".Templates." + type + ".";
            List<string> templateFiles = new List<string>();
            IEnumerable<string> templates = typeof(CodeGeneratorSettings).GetTypeInfo().Assembly.GetManifestResourceNames().Where(x => x.StartsWith(templateSet, StringComparison.OrdinalIgnoreCase));
            templateFiles.AddRange(templates);

            return templateFiles;
        }

        public IEnumerable<CodeGeneratorSettings> GenerateSettings(CodeGeneratorSettingsLoaderDefaults settings, IEnumerable<string> paths)
        {
            var root = Directory.GetCurrentDirectory();

            // we need to multi pass the source files looking for items to load
            var toProcess = new Queue<string>(paths.SelectMany(x => GlobExpander.FindFiles(root, x)));
            List<SimpleSourceFile> sourceFiles = new List<SimpleSourceFile>();
            List<string> processedPaths = new List<string>();
            while (toProcess.Any())
            {
                var path = toProcess.Dequeue();
                processedPaths.Add(path);
                var loaded = Load(path, settings);
                var newPaths = loaded.Includes.Where(x => !processedPaths.Contains(x));
                foreach (var p in newPaths)
                {
                    toProcess.Enqueue(p);
                }
                sourceFiles.Add(loaded);

            }

            var grouped = sourceFiles.GroupBy(x => new
            {
                hash = x.SettingsHash()
            }).ToList();


            return grouped.Select(x =>
            {
                var first = x.First();

                var nspc = "";
                var cn = first.ClassName;
                var idx = first.ClassName.LastIndexOf('.');
                if (idx > 0)
                {
                    cn = first.ClassName.Substring(idx);
                    nspc = first.ClassName.Substring(0, idx);
                }
                cn = cn.Trim('.');
                nspc = nspc.Trim('.');
                var templates = DefaultTemplates(first.OutputPath);
                templates.AddRange(x.First().Templates);
                return new CodeGeneratorSettings
                {
                    ClassName = cn,
                    Namespace = nspc,
                    OutputPath = first.OutputPath,
                    TypeNameDirective = first.TypeNameDirective,
                    SourceFiles = x.Select(p => new NamedSource
                    {
                        Body = p.Body,
                        Path = p.Path
                    }).ToList(),
                    Schema = x.First().SchemaSource,
                    Templates = templates,
                    TemplateSettings = first.TemplateSettings
                };
            }).ToList();
        }

        internal class SimpleSettings
        {
            public string Namespace { get; set; }

            public string Class { get; set; }

            public string Output { get; set; }

            public string Format { get; set; }

            public string TypeNameDirective { get; set; }

            [JsonConverter(typeof(SchemaSourceJsonConverter))]
            public SchemaSource Schema { get; set; }

            [JsonConverter(typeof(SingleOrArrayConverter<string>))]
            public List<string> Template { get; set; }

            public Dictionary<string, string> TemplateSettings { get; set; }

            [JsonConverter(typeof(SingleOrArrayConverter<string>))]
            public List<string> Include { get; set; }

            public bool Root { get; set; } = false;
        }

        public static string GenerateFullPath(string rootFolder, string path)
        {
            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(rootFolder, path);

            }
            return Path.GetFullPath(path);
        }

        private void LoadSettingsTree(SimpleSourceFile file)
        {
            Stack<SimpleSettings> settingsList = new Stack<SimpleSettings>();
            var directory = Path.GetDirectoryName(file.Path);
            while (directory != null)
            {
                var settingsFile = Path.Combine(directory, "gqlsettings.json");
                if (File.Exists(settingsFile))
                {
                    if (ExpandSettings(settingsFile, file))
                    {
                        return;
                    }
                }

                directory = Path.GetDirectoryName(directory);
            }
        }

        static readonly Regex regex = new Regex(@"^\s*#!\s*([a-zA-Z.]+)\s*:\s*(\"".*\""|[^ ]+?)(?:\s|$)", RegexOptions.Multiline | RegexOptions.Compiled);
        private readonly ILogger logger;

        private SimpleSourceFile Load(string path, CodeGeneratorSettingsLoaderDefaults settings)
        {
            // path must be a real full path by here

            var gql = File.ReadAllText(path);

            // lets discover / cache all settings files from this point up the stack and root them out

            var file = new SimpleSourceFile()
            {
                Path = path,
                Body = gql
            };
            var root = Path.GetDirectoryName(path);

            var matches = regex.Matches(gql);
            var pairs = matches.OfType<Match>().Select(m => (key: m.Groups[1].Value.ToLower(), val: m.Groups[2].Value)).ToList();

            foreach (var m in pairs)
            {
                // process them in order to later directives overwrite previous ones
                switch (m.key)
                {
                    case "schema":
                        file.SchemaSource = file.SchemaSource ?? new SchemaSource();
                        file.SchemaSource.Location = GenerateFullPath(root, m.val);
                        break;
                    case "schema.querytype":
                        file.SchemaSource = file.SchemaSource ?? new SchemaSource();
                        file.SchemaSource.QueryType.Add(m.val);
                        break;
                    case "schema.mutationtype":
                        file.SchemaSource = file.SchemaSource ?? new SchemaSource();
                        file.SchemaSource.MutationType.Add(m.val);
                        break;
                    case "output":
                        file.OutputPath = GenerateFullPath(root, m.val);
                        break;
                    case "class":
                        file.ClassName = m.val;
                        break;
                    case "typedirective":
                        file.TypeNameDirective = m.val;
                        break;
                    case "format":
                        file.Format = m.val;
                        break;
                    case "settings":
                        ExpandSettings(GenerateFullPath(root, m.val), file);
                        break;
                    case "template":
                        var templateFiles = GlobExpander.FindFiles(root, m.val);
                        file.Templates.AddRange(templateFiles);
                        break;
                    case "include":
                        var includeFiles = GlobExpander.FindFiles(root, m.val);
                        file.Includes.AddRange(includeFiles);
                        break;
                    default:
                        break;
                }
            }

            LoadSettingsTree(file);


            if (string.IsNullOrWhiteSpace(file.TypeNameDirective))
            {
                file.TypeNameDirective = "__codeGenTypeName";
            }

            if (string.IsNullOrWhiteSpace(file.Format))
            {
                if (!string.IsNullOrWhiteSpace(file.OutputPath))
                {
                    file.Format = Path.GetExtension(file.OutputPath).Trim('.').ToLower();
                }

                if (string.IsNullOrWhiteSpace(file.Format))
                {
                    file.Format = settings.Format;
                }
            }

            if (string.IsNullOrWhiteSpace(file.OutputPath))
            {
                if (string.IsNullOrWhiteSpace(settings.OutputPath))
                {
                    file.OutputPath = file.Path;
                }
                else
                {
                    file.OutputPath = settings.OutputPath;
                }
            }

            if (!Path.GetExtension(file.OutputPath).Trim('.').Equals(file.Format, StringComparison.OrdinalIgnoreCase))
            {
                file.OutputPath += "." + file.Format;
            }

            if (string.IsNullOrWhiteSpace(file.ClassName))
            {
                file.ClassName = Path.GetFileNameWithoutExtension(file.Path);
            }

            settings.FixFile?.Invoke(file);
            file.OutputPath = file.OutputPath.Replace("{classname}", file.ClassName);

            return file;
        }

        private bool ExpandSettings(string path, SimpleSourceFile file)
        {
            var root = Path.GetDirectoryName(path);
            var settingsJson = File.ReadAllText(path);

            var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<SimpleSettings>(settingsJson);
            if (!string.IsNullOrWhiteSpace(settings.Class) && string.IsNullOrWhiteSpace(file.ClassName))
            {
                file.ClassName = settings.Class;
            }
            if (!string.IsNullOrWhiteSpace(settings.TypeNameDirective) && string.IsNullOrWhiteSpace(file.TypeNameDirective))
            {
                file.TypeNameDirective = settings.TypeNameDirective;
            }
            if (!string.IsNullOrWhiteSpace(settings.Output) && string.IsNullOrWhiteSpace(file.OutputPath))
            {
                file.OutputPath = GenerateFullPath(root, settings.Output);
            }
            if (!string.IsNullOrWhiteSpace(settings.Format) && string.IsNullOrWhiteSpace(file.Format))
            {
                file.Format = settings.Format;
            }
            if (settings.Template != null)
            {
                foreach (var t in settings.Template)
                {
                    var templateFiles = GlobExpander.FindFiles(root, t);
                    file.Templates.AddRange(templateFiles);
                }
            }

            if (settings.TemplateSettings != null)
            {
                foreach (var t in settings.TemplateSettings)
                {
                    if(!file.TemplateSettings.TryGetValue(t.Key, out _))
                    {
                        // set if doesn't exist
                        file.TemplateSettings[t.Key] = t.Value;
                    }
                }
            }

            if (settings.Include != null)
            {
                foreach (var t in settings.Include)
                {
                    var files = GlobExpander.FindFiles(root, t);
                    file.Includes.AddRange(files);
                }
            }

            if (string.IsNullOrWhiteSpace(file.SchemaSource?.Location))
            {
                if (settings.Schema != null && !string.IsNullOrWhiteSpace(settings.Schema.Location))
                {
                    file.SchemaSource = settings.Schema;
                    if (file.SchemaSource.SchemaType() != SchemaSource.SchemaTypes.Http)
                    {
                        // we are not and url based location then it must be a path
                        file.SchemaSource.Location = GlobExpander.FindFile(root, file.SchemaSource.Location);
                    }
                }
            }

            return settings.Root;
        }
    }
}
