using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Tocsoft.GraphQLCodeGen.Tests
{
    public class TypescriptCodeGen
    {
        FakeLogger logger;
        CodeGeneratorSettingsLoader settingsLoader;
        public TypescriptCodeGen()
        {
            logger = new FakeLogger();
            settingsLoader = new CodeGeneratorSettingsLoader(logger);
        }

        [Theory]
        [InlineData("Query.gql", "./seg1/target")]
        [InlineData("seg1/seg2/Query.gql", "../target")]
        public async Task CanReferenceRelativePath(string targetQuery, string expected)
        {
            var settings = settingsLoader.GenerateSettings(new CodeGeneratorSettingsLoaderDefaults(), new[] { "./Files/TypescriptCodeGen/" + targetQuery }).Single();

            settings.TemplateSettings["Includes"] = "import { str } from {{ resolve '~/seg1/target' }}";

            CodeGenerator generator = new CodeGenerator(logger, settings);

            await generator.LoadSource();
            generator.Parse();
            generator.Render();

            Assert.Empty(generator.Document.Errors);

            var code = generator.GeneratedCode;

            Assert.Contains(@"import { str } from " + expected, code);
        }

        [Fact]
        public async Task SwitchFlavor()
        {
            var settings = settingsLoader.GenerateSettings(new CodeGeneratorSettingsLoaderDefaults(), new[] { "./Files/TypescriptCodeGen/FunctionFlavor.gql" }).Single();

            settings.TemplateSettings["ApiUrl"] = "https://example.com";

            CodeGenerator generator = new CodeGenerator(logger, settings);

            await generator.LoadSource();
            generator.Parse();
            generator.Render();

            Assert.Empty(generator.Document.Errors);

            var code = generator.GeneratedCode;

            Assert.Contains(@"const url = ""https://example.com"";", code);
            Assert.Contains(@"export function q(", code);
        }

        [Fact]
        public async Task SwitchFlavor_OverrideUrlLoader()
        {
            var settings = settingsLoader.GenerateSettings(new CodeGeneratorSettingsLoaderDefaults(), new[] { "./Files/TypescriptCodeGen/FunctionFlavor.gql" }).Single();

            settings.TemplateSettings["Includes"] = @"import { apiRootUrl } from ""{{ resolve  ""~/src/client/urls"" }}""";
            settings.TemplateSettings["UrlConfig"] = "const url = apiRootUrl;";

            CodeGenerator generator = new CodeGenerator(logger, settings);

            await generator.LoadSource();
            generator.Parse();
            generator.Render();

            Assert.Empty(generator.Document.Errors);

            var code = generator.GeneratedCode;

            Assert.Contains(@"const url = apiRootUrl;", code);
            Assert.Contains(@"export function q(", code);
        }

        [Fact]
        public async Task SwitchFlavorRequiredSettings()
        {
            var settings = settingsLoader.GenerateSettings(new CodeGeneratorSettingsLoaderDefaults(), new[] { "./Files/TypescriptCodeGen/FunctionFlavor.gql" }).Single();


            CodeGenerator generator = new CodeGenerator(logger, settings);

            await generator.LoadSource();
            generator.Parse();
            generator.Render();
            
            Assert.NotEmpty(logger.ErrorMessages);
        }
    }
}
