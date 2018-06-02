using System;
using System.Linq;
using System.Threading.Tasks;
using Tocsoft.GraphQLCodeGen.Cli;
using Xunit;

namespace Tocsoft.GraphQLCodeGen.Tests
{
    public class Includes
    {
        FakeLogger logger;
        CodeGeneratorSettingsLoader settingsLoader;
        public Includes()
        {
            logger = new FakeLogger();
            settingsLoader = new CodeGeneratorSettingsLoader(logger);
        }

        [Theory]
        [InlineData("./Files/Includes/using_includes.gql")]
        [InlineData("./Files/Includes_Settings/using_includes.gql")]
        public async Task LoadFieldsFromFragments(string rootQuery)
        {
            var settings = settingsLoader.GenerateSettings(new CodeGeneratorSettingsLoaderDefaults(), new[] { rootQuery });

            CodeGenerator generator = new CodeGenerator(logger, settings.Single());

            await generator.LoadSource();
            generator.Parse();

            var model = generator.Model;

            var resultType = Assert.Single(model.Types.Where(x => x.Name == "user" && x.IsInterface));
        }


        [Theory]
        [InlineData("./Files/Includes/using_includes.gql")]
        [InlineData("./Files/Includes_Settings/using_includes.gql")]
        public async Task RenderInterfaces(string rootQuery)
        {
            var settings = settingsLoader.GenerateSettings(new CodeGeneratorSettingsLoaderDefaults(), new[] { rootQuery });

            CodeGenerator generator = new CodeGenerator(logger, settings.Single());

            await generator.LoadSource();
            generator.Parse();
            generator.Render();
            var code = generator.GeneratedCode;

            Assert.Contains("interface IUser", code);
            Assert.Contains("class UserResult : IUser", code);
            Assert.Contains(@"query ($login:ID!){
  User(id: $login){
    ...user
  }
}
fragment user on User {
    id,
  	username
}".Trim().Replace("\r",""), code.Replace("\r", ""));
        }
    }

}
