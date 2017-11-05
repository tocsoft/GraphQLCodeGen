using GraphQLParser;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Tocsoft.GraphQLCodeGen.SchemaIntrospection;
using static Tocsoft.GraphQLCodeGen.IntrospectedSchemeParser;

namespace Tocsoft.GraphQLCodeGen
{
    public class CodeGenerator
    {
        private readonly CodeGeneratorSettings settings;
        private readonly IEnumerable<SchemaIntrospection.IIntrosepctionProvider> introspectionProviders;

        public CodeGenerator(CodeGeneratorSettings settings)
            : this(settings, new IIntrosepctionProvider[] {
                new SchemaIntrospection.JsonIntrospection(),
                new SchemaIntrospection.HttpIntrospection(),
                new SchemaIntrospection.SchemaFileIntrospection()
            })
        {
        }

        public CodeGenerator(CodeGeneratorSettings settings, IEnumerable<SchemaIntrospection.IIntrosepctionProvider> introspectionProviders)
        {
            this.settings = settings;
            this.introspectionProviders = introspectionProviders;
        }

        public async Task GenerateAsync()
        {
            var provider = introspectionProviders.Single(x => x.SchemaType == settings.Schema.SchemaType());

            var schema = await provider.LoadSchema(settings.Schema);
            // we need to load in the scheme
            var sources = new List<NamedSource>();
            sources.Add(new NamedSource() { Path = settings.Schema.Location, Body = schema });

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

        public string Template { get; set; }
        public string SourcePath { get; set; }
        public CodeGeneratorSettingsLoader.SchemaSource Schema { get; internal set; }
    }

    public class CodeGeneratorSettingsLoader
    {
        public CodeGeneratorSettingsLoader()
        {

        }

        public IEnumerable<CodeGeneratorSettings> LoadFromPath(string path)
        {
            var paths = GetPaths(Directory.GetCurrentDirectory(), path);

            return paths.SelectMany(LoadFromResolvedPath).ToList();
        }

        private IEnumerable<CodeGeneratorSettings> LoadFromResolvedPath(string path)
        {
            // rootPath
            var dir = Path.GetDirectoryName(path);

            var json = File.ReadAllText(path);
            
            var simpleList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SimpleSettings>>(json, new SingleOrArrayConverter<SimpleSettings>());
            foreach (var simple in simpleList) {
                var settings = new CodeGeneratorSettings();

                if (simple.Schema.SchemaType() != SchemaSource.SchemaTypes.Http)
                {
                    // we are not and url based location then it must be a path
                    simple.Schema.Location = GetPath(dir, simple.Schema.Location);
                }

                settings.Schema = simple.Schema;
                settings.OutputPath = GetPath(dir, simple.Output);
                settings.SourcePaths = GetPaths(dir, simple.Source);
                settings.ClassName = simple.Classname;
                settings.Namespace = simple.Namespace;
                settings.Template = simple.Template;
                settings.SettingsPath = path;
                yield return settings;
            }
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

            [JsonConverter(typeof(SchemaSourceJsonConverter))]
            public SchemaSource Schema { get; set; }

            public string Template { get; set; }
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
            public string[] QueryType { get; set; }

            /// <summary>
            /// for dll based introspection this is the list of types that make up the queries
            /// </summary>
            public string[] MutationType { get; set; }

            public SchemaTypes SchemaType()
            {
                if (Location.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    return SchemaTypes.Http;
                }

                if (Location.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || Location.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    return SchemaTypes.Dll;
                }

                if (Location.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
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
        }

        public class SchemaSourceJsonConverter : JsonConverter
        {
            public override bool CanWrite => false;

            public override bool CanConvert(Type objectType)
            {
                return typeof(SchemaSource) == objectType;
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                JToken token = JToken.Load(reader);
                var result = (existingValue as SchemaSource ?? new SchemaSource());

                if (token is Newtonsoft.Json.Linq.JObject obj)
                {
                    var temp = token.ToObject<SchemaSource>();

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
}
