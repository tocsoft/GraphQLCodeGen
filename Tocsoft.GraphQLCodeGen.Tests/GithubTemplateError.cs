using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Tocsoft.GraphQLCodeGen.Tests
{
    public class GithubTemplateError
    {
        FakeLogger logger;
        CodeGeneratorSettingsLoader settingsLoader;
        public GithubTemplateError()
        {
            logger = new FakeLogger();
            settingsLoader = new CodeGeneratorSettingsLoader(logger);
        }

        [Fact]
        public async Task DoesNotErrorRendering()
        {
            var logger = new FakeLogger();
            var settingsLoader = new CodeGeneratorSettingsLoader(logger);

            var paths = "./Files/GithubTemplateError/History.gql";

            var settings = settingsLoader.GenerateSettings(new CodeGeneratorSettingsLoaderDefaults(), new[] { paths });

            var generator = new CodeGenerator(logger, settings.Single());

            await generator.LoadSource();
            generator.Parse();
            generator.Render();
            var code = generator.GeneratedCode;
            Assert.Empty(logger.ErrorMessages);
        }

        [Fact]
        public async Task CorrectExceptionTestData()
        {
            var logger = new FakeLogger();
            var settingsLoader = new CodeGeneratorSettingsLoader(logger);

            var paths = "./Files/GithubTemplateError/History.gql";

            var settings = settingsLoader.GenerateSettings(new CodeGeneratorSettingsLoaderDefaults(), new[] { paths });

            var generator = new CodeGenerator(logger, settings.Single());

            await generator.LoadSource();
            generator.Parse();
            generator.Render();

            Assert.Contains("public IEnumerable<string> ErrorMessages { get; private set; }", generator.GeneratedCode);
        }
    }
}
