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
            this.GeneratedCode = new TemplateEngine(this.settings.Templates, this.settings.TemplateSettings, logger, Model).Generate();
        }

        internal void Parse()
        {
            Document = IntrospectedSchemeParser.Parse(Sources, this.settings);

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
}
