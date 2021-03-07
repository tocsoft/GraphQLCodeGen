using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Tocsoft.GraphQLCodeGen.Tests
{
    public class Issue19
    {
        [Fact]
        public async Task GenerateNamedMutationIsCorrect()
        {
            var logger = new FakeLogger();
            var settingsLoader = new CodeGeneratorSettingsLoader(logger);

            var paths = "./Files/Issue_19/named.gql";

            var settings = settingsLoader.GenerateSettings(new CodeGeneratorSettingsLoaderDefaults(), new[] { paths });

            var generator = new CodeGenerator(logger, settings.Single());

            await generator.LoadSource();
            generator.Parse();
            generator.Render();
            var code = generator.GeneratedCode;

            Assert.Contains("mutation IfNamedUseNameInstead($repositoyId: ID!)", code);
        }

        [Fact]
        public async Task GenerateNonNamedMutationIsCorrect()
        {
            var logger = new FakeLogger();
            var settingsLoader = new CodeGeneratorSettingsLoader(logger);

            var paths = "./Files/Issue_19/notNamed.gql";

            var settings = settingsLoader.GenerateSettings(new CodeGeneratorSettingsLoaderDefaults(), new[] { paths });

            var generator = new CodeGenerator(logger, settings.Single());

            await generator.LoadSource();
            generator.Parse();
            generator.Render();
            var code = generator.GeneratedCode;

            Assert.Contains("mutation($repositoyId: ID!)", code);
        }
    }
}
