using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Tocsoft.GraphQLCodeGen.Tests
{
    public class SchemaFiles
    {
        FakeLogger logger;
        CodeGeneratorSettingsLoader settingsLoader;
        public SchemaFiles()
        {
            logger = new FakeLogger();
            settingsLoader = new CodeGeneratorSettingsLoader(logger);
        }

        [Fact]
        public async Task CanProcessSchemaWothDescriptions()
        {
            var settings = settingsLoader.GenerateSettings(new CodeGeneratorSettingsLoaderDefaults(), new[] { "./Files/SchemaFiles/Query.gql" }).Single();

            settings.TemplateSettings["InterfaceBase"] = "IInterfaceBase";

            CodeGenerator generator = new CodeGenerator(logger, settings);

            await generator.LoadSource();
            generator.Parse();
            generator.Render();

            Assert.Empty(generator.Document.Errors);

            var code = generator.GeneratedCode;

            Assert.Contains(@"ITest : IInterfaceBase", code);
        }

    }
}
