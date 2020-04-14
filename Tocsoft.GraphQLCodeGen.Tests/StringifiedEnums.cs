using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Tocsoft.GraphQLCodeGen.Tests
{
    public class StringifiedEnums
    {
        FakeLogger logger;
        CodeGeneratorSettingsLoader settingsLoader;
        public StringifiedEnums()
        {
            logger = new FakeLogger();
            settingsLoader = new CodeGeneratorSettingsLoader(logger);
        }

        [Fact]
        public async Task SettingExplicitlyTrue()
        {
            var settings = settingsLoader.GenerateSettings(new CodeGeneratorSettingsLoaderDefaults(), new[] { "./Files/StringifiedEnums/Query.gql" }).Single();

            settings.TemplateSettings["StringifyEnums"] = "true";

            CodeGenerator generator = new CodeGenerator(logger, settings);

            await generator.LoadSource();
            generator.Parse();
            generator.Render();

            Assert.Empty(generator.Document.Errors);

            var code = generator.GeneratedCode;

            Assert.Contains(@"public class Episode : IStringifiedEnum", code);
        }

        [Fact]
        public async Task SettingImplicitlyTrue()
        {
            var settings = settingsLoader.GenerateSettings(new CodeGeneratorSettingsLoaderDefaults(), new[] { "./Files/StringifiedEnums/Query.gql" }).Single();

            CodeGenerator generator = new CodeGenerator(logger, settings);

            await generator.LoadSource();
            generator.Parse();
            generator.Render();

            Assert.Empty(generator.Document.Errors);

            var code = generator.GeneratedCode;

            Assert.Contains(@"public class Episode : IStringifiedEnum", code);
        }

        [Fact]
        public async Task SettingExplicitlyFalse()
        {
            var settings = settingsLoader.GenerateSettings(new CodeGeneratorSettingsLoaderDefaults(), new[] { "./Files/StringifiedEnums/Query.gql" }).Single();

            settings.TemplateSettings["StringifyEnums"] = "false";

            CodeGenerator generator = new CodeGenerator(logger, settings);

            await generator.LoadSource();
            generator.Parse();
            generator.Render();

            Assert.Empty(generator.Document.Errors);

            var code = generator.GeneratedCode;

            Assert.DoesNotContain(@"IStringifiedEnum", code);

            Assert.Contains(@"public enum Episode", code);
        }
    }
}
