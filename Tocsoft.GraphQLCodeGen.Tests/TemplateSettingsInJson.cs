using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Tocsoft.GraphQLCodeGen.Tests
{
    public class TemplateSettingsInJson
    {
        FakeLogger logger;
        CodeGeneratorSettingsLoader settingsLoader;
        public TemplateSettingsInJson()
        {
            logger = new FakeLogger();
            settingsLoader = new CodeGeneratorSettingsLoader(logger);
        }

        [Fact]
        public async Task CanSetInterfaceBaseFromSettings()
        {
            var settings = settingsLoader.GenerateSettings(new CodeGeneratorSettingsLoaderDefaults(), new[] { "./Files/TemplateSettingsInJson/Query.gql" }).Single();

            settings.TemplateSettings["InterfaceBase"] = "IInterfaceBase";

            CodeGenerator generator = new CodeGenerator(logger, settings);

            await generator.LoadSource();
            generator.Parse();
            generator.Render();

            Assert.Empty(generator.Document.Errors);

            var code = generator.GeneratedCode;

            Assert.Contains(@"ITest : IInterfaceBase", code);
        }

        [Fact]
        public async Task InterfaceBaseSectionSkippedIfSettingBlank()
        {
            var settings = settingsLoader.GenerateSettings(new CodeGeneratorSettingsLoaderDefaults(), new[] { "./Files/TemplateSettingsInJson/Query.gql" }).Single();

            settings.TemplateSettings["InterfaceBase"] = "";

            CodeGenerator generator = new CodeGenerator(logger, settings);

            await generator.LoadSource();
            generator.Parse();
            generator.Render();

            Assert.Empty(generator.Document.Errors);

            var code = generator.GeneratedCode;

            Assert.DoesNotContain(@"ITest :", code);
        }
        [Fact]
        public void InterfaceBaseSectionReadFromJson()
        {
            var settings = settingsLoader.GenerateSettings(new CodeGeneratorSettingsLoaderDefaults(), new[] { "./Files/TemplateSettingsInJson/Query.gql" }).Single();

            Assert.Equal("IFooBase", settings.TemplateSettings["interfaceBase"]);
        }
    }
}
