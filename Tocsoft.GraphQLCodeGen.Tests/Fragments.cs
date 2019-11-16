using System;
using System.Linq;
using System.Threading.Tasks;
using Tocsoft.GraphQLCodeGen.Cli;
using Xunit;

namespace Tocsoft.GraphQLCodeGen.Tests
{
    public class ParseFieldsFromFragments
    {
        FakeLogger logger;
        CodeGeneratorSettingsLoader settingsLoader;
        public ParseFieldsFromFragments()
        {
            logger = new FakeLogger();
            settingsLoader = new CodeGeneratorSettingsLoader(logger);
        }

        [Fact]
        public async Task LoadFieldsFromFragments()
        {
            var settings = settingsLoader.GenerateSettings(new CodeGeneratorSettingsLoaderDefaults(), new[] { "./Files/Fragements/using_fragment.gql" });

            CodeGenerator generator = new CodeGenerator(logger, settings.Single());

            await generator.LoadSource();
            generator.Parse();

            var model = generator.Model;

            var resultType = Assert.Single(model.Types.Where(x => x.Name == "user" && x.IsInterface));
        }

        [Fact]
        public async Task RenderInterfaces()
        {
            var settings = settingsLoader.GenerateSettings(new CodeGeneratorSettingsLoaderDefaults(), new[] { "./Files/Fragements/using_fragment.gql" });

            CodeGenerator generator = new CodeGenerator(logger, settings.Single());

            await generator.LoadSource();
            generator.Parse();
            generator.Render();
            var code = generator.GeneratedCode;

            Assert.Contains("interface IUser", code);
            Assert.Contains("class UserResult : IUser", code);
            Assert.Contains(@"query ($login: ID!) {
  User(id: $login){
    ...user
  }
}
fragment user on User {
  id
  username
}".Trim().Replace("\r",""), code.Replace("\r", ""));
        }
    }

}
