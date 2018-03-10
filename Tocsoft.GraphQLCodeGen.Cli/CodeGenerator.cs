using GraphQLParser;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tocsoft.GraphQLCodeGen.Cli;
using Tocsoft.GraphQLCodeGen.SchemaIntrospection;
using static Tocsoft.GraphQLCodeGen.IntrospectedSchemeParser;

namespace Tocsoft.GraphQLCodeGen
{
    internal class CodeGenerator
    {
        private readonly CodeGeneratorSettings settings;
        private readonly IEnumerable<SchemaIntrospection.IIntrosepctionProvider> introspectionProviders;
        private readonly ILogger logger;

        public CodeGenerator(ILogger logger, CodeGeneratorSettings settings)
            : this(logger, settings, new IIntrosepctionProvider[] {
                new SchemaIntrospection.JsonIntrospection(),
                new SchemaIntrospection.HttpIntrospection(),
                new SchemaIntrospection.DllIntrospection(),
                new SchemaIntrospection.SchemaFileIntrospection()
            })
        {
        }

        public CodeGenerator(ILogger logger, CodeGeneratorSettings settings, IEnumerable<SchemaIntrospection.IIntrosepctionProvider> introspectionProviders)
        {
            this.logger = logger;
            this.settings = settings;
            this.introspectionProviders = introspectionProviders;
        }

        internal IEnumerable<NamedSource> Sources { get; set; }
        internal ObjectModel.GraphQLDocument Document { get; set; }
        internal Models.ViewModel Model { get; set; }
        internal string GeneratedCode { get; set; }
        internal bool HasParsingErrors { get; set; }

        internal async Task LoadSource()
        {
            IIntrosepctionProvider provider = this.introspectionProviders.Single(x => x.SchemaType == this.settings.Schema.SchemaType());

            string schema = await provider.LoadSchema(this.settings.Schema);
            // we need to load in the scheme
            List<NamedSource> sources = new List<NamedSource>();
            sources.Add(new NamedSource() { Path = this.settings.Schema.Location, Body = schema });

            foreach (var s in this.settings.SourceFiles)
            {
                sources.Add(s);
            }

            Sources = sources;
        }

        internal void Export()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(this.settings.OutputPath));
            File.WriteAllText(this.settings.OutputPath, GeneratedCode);
        }

        internal void Render()
        {
            this.GeneratedCode = new TemplateEngine(this.settings.Templates, logger).Generate(Model);
        }

        internal void Parse()
        {
            Document = IntrospectedSchemeParser.Parse(Sources);

            if (Document.Errors.Any())
            {
                foreach (var error in Document.Errors)
                {
                    logger.Error(error.ToString());
                }
                HasParsingErrors = true;
                return;
            }

            Model = new Models.ViewModel(Document, this.settings);

            HasParsingErrors = false;
        }

        public async Task<bool> GenerateAsync()
        {
            await LoadSource();

            Parse();

            if (HasParsingErrors)
            {
                return false;
            }

            Render();

            Export();

            return true;
        }
    }

    public class CodeGeneratorSettings
    {
        public string Namespace { get; set; }
        public string ClassName { get; set; }
        public string OutputPath { get; set; }
        public string Format { get; set; }
        public IEnumerable<NamedSource> SourceFiles { get; set; }

        public IEnumerable<string> Templates { get; set; }
        internal SchemaSource Schema { get; set; }
    }

    internal class CodeGeneratorSettingsLoaderDefaults
    {
        public string Format { get; set; }

        public string OutputPath { get; set; }

        public Action<SimpleSourceFile> FixFile { get; set; }
    }

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
            IEnumerable<string> sourceFilePaths = paths.SelectMany(x => GlobExpander.FindFiles(root, x));

            var sourceFiles = sourceFilePaths.Select(x => Load(x, settings)).ToList();

            var grouped = sourceFiles.GroupBy(x => new
            {
                x.ClassName,
                x.OutputPath,
                x.Format,
                hash = x.SettingsHash()
            });


            return grouped.Select(x =>
            {
                var nspc = "";
                var cn = x.Key.ClassName;
                var idx = x.Key.ClassName.LastIndexOf('.');
                if (idx > 0)
                {
                    cn = x.Key.ClassName.Substring(idx);
                    nspc = x.Key.ClassName.Substring(0, idx);
                }
                cn = cn.Trim('.');
                nspc = nspc.Trim('.');
                var templates = DefaultTemplates(x.Key.OutputPath);
                templates.AddRange(x.First().Templates);
                return new CodeGeneratorSettings
                {
                    ClassName = cn,
                    Namespace = nspc,
                    OutputPath = x.Key.OutputPath,
                    SourceFiles = x.Select(p => new NamedSource
                    {
                        Body = p.Body,
                        Path = p.Path
                    }).ToList(),
                    Schema = x.First().SchemaSource,
                    Templates = templates
                };
            }).ToList();
        }

        internal class SimpleSettings
        {
            public string Namespace { get; set; }

            public string Class { get; set; }

            public string Output { get; set; }

            public string Format { get; set; }

            [JsonConverter(typeof(SchemaSourceJsonConverter))]
            public SchemaSource Schema { get; set; }

            [JsonConverter(typeof(SingleOrArrayConverter<string>))]
            public List<string> Template { get; set; }

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
            var rooted = false;
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
                    default:
                        break;
                }
            }

            LoadSettingsTree(file);

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

    class SingleOrArrayConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(List<T>));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);
            if (token.Type == JTokenType.Array)
            {
                return token.ToObject<List<T>>();
            }
            return new List<T> { token.ToObject<T>() };
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    [DebuggerDisplay("{Path}")]
    internal class SimpleSourceFile
    {
        public string Path { get; set; }
        public string Body { get; set; }
        public string Format { get; set; }
        public string ClassName { get; set; }
        public string OutputPath { get; set; }
        public List<string> Templates { get; set; } = new List<string>();
        public SchemaSource SchemaSource { get; set; }

        internal string SettingsHash()
        {
            StringBuilder sb = new StringBuilder();


            sb.Append(ClassName);
            sb.Append("~#~");
            sb.Append(OutputPath);
            sb.Append("~#~");
            sb.Append(Format);
            sb.Append("~#~");
            if (Templates != null)
            {
                foreach (var t in Templates)
                {
                    sb.Append(t);
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


    public class SchemaSource
    {
        // we guess based on the string value what type of introspectino to do,
        // if its http/https then its a http introspection
        // if its a file path its eather a schema or json file
        // if its a dll then its a GraphQL conventions schema and other paramaters kick in
        public string Location { get; set; }

        /// <summary>
        /// for http based introspection then we use the headers
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// for dll based introspection this is the list of types that make up the queries
        /// </summary>
        public List<string> QueryType { get; set; } = new List<string>();

        /// <summary>
        /// for dll based introspection this is the list of types that make up the queries
        /// </summary>
        public List<string> MutationType { get; set; } = new List<string>();

        public SchemaTypes SchemaType()
        {
            if (this.Location.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return SchemaTypes.Http;
            }

            if (this.Location.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || this.Location.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                return SchemaTypes.Dll;
            }

            if (this.Location.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                return SchemaTypes.Json;
            }

            return SchemaTypes.GraphQLSchemaFile;
        }

        public enum SchemaTypes
        {
            Http,
            Dll,
            Json,
            GraphQLSchemaFile
        }

        internal string SettingsHash()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(Location);
            sb.Append("~#~");
            if (QueryType != null)
            {
                foreach (var t in QueryType)
                {
                    sb.Append(t);
                    sb.Append("~#~");
                }
            }
            if (MutationType != null)
            {
                foreach (var t in MutationType)
                {
                    sb.Append(t);
                    sb.Append("~#~");
                }
            }

            return sb.ToString();
        }
    }

    internal class SchemaSourceJsonConverter : JsonConverter
    {
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType)
        {
            return typeof(SchemaSource) == objectType;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);
            SchemaSource result = (existingValue as SchemaSource ?? new SchemaSource());

            if (token is Newtonsoft.Json.Linq.JObject obj)
            {
                SchemaSource temp = token.ToObject<SchemaSource>();

                result.Location = temp.Location;
                result.MutationType = temp.MutationType;
                result.QueryType = temp.QueryType;
                result.Headers = temp.Headers;
            }
            else if (token is Newtonsoft.Json.Linq.JValue value)
            {
                result.Location = value.Value.ToString();
            }

            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
