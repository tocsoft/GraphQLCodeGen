using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Tocsoft.GraphQLCodeGen.Tests
{
    public class StringifiedEnumsSystemTextJson
    {
        private readonly CodeGeneratorTester tester;

        public StringifiedEnumsSystemTextJson()
        {
            tester = new CodeGeneratorTester();
        }

        [Fact]
        public async Task SerilizesMutationValuesCorrectly_Stringified()
        {
            tester.AddQuery("./Files/StringifiedEnums/Mutation.gql");
            tester.Configure(s =>
            {
                s.TemplateSettings["StringifyEnums"] = "true";
                s.TemplateSettings["JsonConverter"] = "System.Text.Json";
            });

            var query = await tester.ExecuteClient("Sample.Client.Test", @"MutationQAsync(Episode.Newhope)");

            Assert.Equal("NEWHOPE", query.Variables["emp"]);
        }

        [Fact]
        public async Task SerilizesMutationValuesCorrectly_Enum()
        {
            tester.AddQuery("./Files/StringifiedEnums/Mutation.gql");
            tester.Configure(s =>
            {
                s.TemplateSettings["StringifyEnums"] = "false";
                s.TemplateSettings["JsonConverter"] = "System.Text.Json";
            });

            var query = await tester.ExecuteClient("Sample.Client.Test", @"MutationQAsync(Episode.NEWHOPE)");

            Assert.Equal("NEWHOPE", query.Variables["emp"]);
        }

        [Fact]
        public async Task SettingExplicitlyTrue()
        {
            tester.AddQuery("./Files/StringifiedEnums/Query.gql");
            tester.Configure(s =>
            {
                s.TemplateSettings["StringifyEnums"] = "true";
                s.TemplateSettings["JsonConverter"] = "System.Text.Json";
            });

            var code = await tester.Generate();

            Assert.Contains(@"[System.Text.Json.Serialization.JsonConverter(typeof(Episode.CustomJsonStringifiedEnumConverter))]", code);
            Assert.Contains(@"public class Episode", code);

            await tester.Verify();
        }

        [Fact]
        public async Task SettingImplicitlyTrue()
        {
            tester.AddQuery("./Files/StringifiedEnums/Query.gql");
            tester.Configure(s =>
            {
                s.TemplateSettings["JsonConverter"] = "System.Text.Json";
            });
            var code = await tester.Generate();

            Assert.Contains(@"[System.Text.Json.Serialization.JsonConverter(typeof(Episode.CustomJsonStringifiedEnumConverter))]", code);
            Assert.Contains(@"public class Episode", code);

            await tester.Verify();
        }

        [Fact]
        public async Task SettingExplicitlyFalse()
        {
            tester.AddQuery("./Files/StringifiedEnums/Query.gql");
            tester.Configure(s =>
            {
                s.TemplateSettings["StringifyEnums"] = "false";
                s.TemplateSettings["JsonConverter"] = "System.Text.Json";
            });

            var code = await tester.Generate();

            Assert.Contains(@"[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]", code);
            Assert.DoesNotContain(@"IStringifiedEnum", code);

            Assert.Contains(@"public enum Episode", code);

            await tester.Verify();
        }
    }
}
