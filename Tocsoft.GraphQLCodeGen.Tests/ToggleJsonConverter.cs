using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Tocsoft.GraphQLCodeGen.Tests
{
    public class ToggleJsonConverter
    {
        FakeLogger logger;
        CodeGeneratorSettingsLoader settingsLoader;
        public ToggleJsonConverter()
        {
            logger = new FakeLogger();
            settingsLoader = new CodeGeneratorSettingsLoader(logger);
        }

        [Fact]
        public async Task NewtonsoftJsonConverter()
        {
            var tester = new CodeGeneratorTester();
            tester.AddQuery("./Files/ToggleJsonConverter/Query.gql");

            tester.Configure(x => x.TemplateSettings.Add("JsonConverter", "Newtonsoft.Json"));
            var code = await tester.Generate();

            Assert.Contains("using Newtonsoft.Json;", code);
            Assert.Contains("[JsonProperty(\"nullable\")]", code);
        }

        [Fact]
        public async Task SystemTextJsonConverter()
        {
            var tester = new CodeGeneratorTester();
            tester.AddQuery("./Files/ToggleJsonConverter/Query.gql");

            tester.Configure(x => x.TemplateSettings.Add("JsonConverter", "System.Text.Json"));
            var code = await tester.Generate();

            Assert.DoesNotContain("using Newtonsoft.Json;", code);
            Assert.DoesNotContain("[JsonProperty(\"nullable\")]", code);

            Assert.Contains("using System.Text.Json;", code);
            Assert.Contains("[System.Text.Json.Serialization.JsonPropertyName(\"nullable\")]", code);
        }

        [Fact]
        public async Task NewtonsoftJsonConverter_Executes()
        {
            var tester = new CodeGeneratorTester();
            tester.AddQuery("./Files/ToggleJsonConverter/Query.gql");

            tester.Configure(x => x.TemplateSettings.Add("JsonConverter", "Newtonsoft.Json"));
            tester.ConfigureResponse(q => {
                return new {
                    nullable = (string)null,
                    nonnullable = "EMPIRE"
                };
            });
            var code = await tester.Generate();
            var result = await tester.ExecuteClient();
        }

        [Fact]
        public async Task SystemTextJsonConverter_Executes()
        {
            var tester = new CodeGeneratorTester();
            tester.AddQuery("./Files/ToggleJsonConverter/Query.gql");

            tester.Configure(x => x.TemplateSettings.Add("JsonConverter", "System.Text.Json"));

            tester.ConfigureResponse(q => {
                return new
                {
                    nullable = (string)null,
                    nonnullable = "EMPIRE"
                };
            });
            var code = await tester.Generate();
            var result = await tester.ExecuteClient();
        }

    }
}
