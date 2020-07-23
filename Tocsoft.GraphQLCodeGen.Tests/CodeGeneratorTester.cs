using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tocsoft.GraphQLCodeGen.ObjectModel;
using Xunit;

namespace Tocsoft.GraphQLCodeGen.Tests
{
    public class CodeGeneratorTester
    {
        private readonly FakeLogger logger;
        private readonly CodeGeneratorSettingsLoader settingsLoader;
        private List<string> queries = new List<string>();
        private Action<CodeGeneratorSettings> configAction;
        private Func<GraphQlQuery, object> httpIntercepter;
        private FakeHttpClient httpClient;

        public CodeGeneratorTester()
        {

            this.logger = new FakeLogger();
            this.settingsLoader = new CodeGeneratorSettingsLoader(logger);
        }

        public string GeneratedCode { get; private set; }
        internal IEnumerable<GraphQLError> Errors { get; private set; }

        public void AddQuery(string queryPath)
        {
            queries.Add(queryPath);
        }
        public void Configure(Action<CodeGeneratorSettings> configAction)
        {
            this.configAction = configAction;
        }
        public void ConfigureResponse(Func<GraphQlQuery, object> httpIntercepter)
        {
            this.httpIntercepter = httpIntercepter;
        }

        private bool generated = false;

        private string clientClass = null;
        private string defaultOperationName;

        public async Task<string> Generate()
        {
            if (generated) return this.GeneratedCode;

            generated = true;

            var settings = settingsLoader.GenerateSettings(new CodeGeneratorSettingsLoaderDefaults(), queries).Single();

            configAction?.Invoke(settings);

            CodeGenerator generator = new CodeGenerator(logger, settings);


            await generator.LoadSource();

            generator.Parse();
            if (!generator.HasParsingErrors)
            {
                generator.Render();
            }

            this.GeneratedCode = generator.GeneratedCode;

            this.Errors = generator.Document.Errors;
            this.clientClass = $"{generator.Model.Namespace}.{generator.Model.ClassName}";

            this.defaultOperationName = generator.Model.Operations.First().Name.ToPascalCase() + "Async";

            this.clientClass = $"{generator.Model.Namespace}.{generator.Model.ClassName}";

            return this.GeneratedCode;
        }

        public async Task Verify()
        {
            await Generate();

            Assert.Empty(Errors);

            if (this.httpClient != null)
            {
                this.httpClient.VerifyAll();
            }
        }

        public Task<GraphQlQuery> ExecuteClient()
            => ExecuteClient(this.clientClass, $"{this.defaultOperationName}()");

        public Task<GraphQlQuery> ExecuteClient(string code)
            => ExecuteClient(this.clientClass, code);

        public async Task<GraphQlQuery> ExecuteClient(string clientName, string code)
        {
            var generatedCode = await Generate();
            code = code.Trim();

            var finalCode = $@"
using static {clientName};

public class _testClass{{
    public static async System.Threading.Tasks.Task Execute({clientName} client){{
        await client.{code};
    }}
}}
";
            this.httpClient = FakeHttpClient.Create();

            GraphQlQuery query = null;
            this.httpClient.SetupGraphqlRequest((q) =>
            {
                query = q;
                return this.httpIntercepter?.Invoke(q) ?? new { };
            });

            var assembly = new Compiler().Compile(generatedCode, finalCode);

            var clientType = assembly.GetType(clientName);
            var client = Activator.CreateInstance(clientType, this.httpClient);
            var executeMethod = assembly.GetType("_testClass").GetMethod("Execute");

            await (executeMethod.Invoke(null, new[] { client }) as Task);

            this.httpClient.VerifyAll();

            return query;
        }

    }
}
