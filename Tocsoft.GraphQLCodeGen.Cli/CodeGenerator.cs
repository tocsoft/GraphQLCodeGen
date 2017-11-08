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
                new SchemaIntrospection.DllIntrospection(),
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
            IIntrosepctionProvider provider = this.introspectionProviders.Single(x => x.SchemaType == this.settings.Schema.SchemaType());

            string schema = await provider.LoadSchema(this.settings.Schema);
            // we need to load in the scheme
            List<NamedSource> sources = new List<NamedSource>();
            sources.Add(new NamedSource() { Path = this.settings.Schema.Location, Body = schema });

            foreach (string s in this.settings.SourcePaths)
            {
                sources.Add(new NamedSource() { Path = s, Body = File.ReadAllText(s) });
            }
            // we want to track the file that the operation is loaded from
            // lets make a locatino index look up table and provide it
            ObjectModel.GraphQLDocument doc = Parse(sources);

            Models.ViewModel model = new Models.ViewModel(doc, this.settings);

            string fileResult = new TemplateEngine(this.settings.Templates).Generate(model);

            Directory.CreateDirectory(Path.GetDirectoryName(this.settings.OutputPath));
            File.WriteAllText(this.settings.OutputPath, fileResult);
        }

    }

    public class CodeGeneratorSettings
    {
        public string SettingsPath { get; set; }
        public string Namespace { get; set; }
        public string ClassName { get; set; }
        public string OutputPath { get; set; }
        public IEnumerable<string> SourcePaths { get; set; }

        public IEnumerable<string> Templates { get; set; }
        public string SourcePath { get; set; }
        public CodeGeneratorSettingsLoader.SchemaSource Schema { get; internal set; }
    }

    public class CodeGeneratorSettingsLoader
    {
        public CodeGeneratorSettingsLoader()
        {

        }

        public IEnumerable<CodeGeneratorSettings> LoadFromPaths(IEnumerable<string> paths)
        {
            return paths.SelectMany(x => LoadFromPath(x)).ToList();
        }

        public IEnumerable<CodeGeneratorSettings> LoadFromPath(string path)
        {
            IEnumerable<string> paths = GlobExpander.FindFiles(Directory.GetCurrentDirectory(), path);

            return paths.SelectMany(this.LoadFromResolvedPath).ToList();
        }

        private IEnumerable<CodeGeneratorSettings> LoadFromResolvedPath(string path)
        {
            // rootPath
            string dir = Path.GetDirectoryName(path);

            string json = File.ReadAllText(path);

            List<SimpleSettings> simpleList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SimpleSettings>>(json, new SingleOrArrayConverter<SimpleSettings>());
            foreach (SimpleSettings simple in simpleList)
            {
                CodeGeneratorSettings settings = new CodeGeneratorSettings();

                if (simple.Schema.SchemaType() != SchemaSource.SchemaTypes.Http)
                {
                    // we are not and url based location then it must be a path
                    simple.Schema.Location = GlobExpander.FindFile(dir, simple.Schema.Location);
                }

                settings.Schema = simple.Schema;
                settings.OutputPath = Path.Combine(dir, simple.Output);
                settings.SourcePaths = GlobExpander.FindFiles(dir, simple.Source);
                settings.ClassName = simple.Classname;
                settings.Namespace = simple.Namespace;

                // we create a list of paths/resources based on the collection of named templates and the default set loaded in from the source file
                string type = Path.GetExtension(settings.OutputPath).Trim('.').ToLower();
                TypeInfo info = typeof(CodeGeneratorSettings).GetTypeInfo();
                string templateSet = info.Namespace + ".Templates." + type + ".";
                List<string> templateFiles = new List<string>();
                IEnumerable<string> templates = typeof(CodeGeneratorSettings).GetTypeInfo().Assembly.GetManifestResourceNames().Where(x => x.StartsWith(templateSet, StringComparison.OrdinalIgnoreCase));
                templateFiles.AddRange(templates);

                List<string> templatePaths = simple.Template.SelectMany(x => GlobExpander.FindFiles(dir, x)).ToList();
                templateFiles.AddRange(templatePaths);

                settings.Templates = templateFiles;
                settings.SettingsPath = path;
                yield return settings;
            }
        }
        

        public class SimpleSettings
        {
            public string Namespace { get; set; }

            public string Classname { get; set; }

            public string Output { get; set; }

            public string Source { get; set; }

            [JsonConverter(typeof(SchemaSourceJsonConverter))]
            public SchemaSource Schema { get; set; }

            [JsonConverter(typeof(SingleOrArrayConverter<string>))]
            public List<string> Template { get; set; }
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
