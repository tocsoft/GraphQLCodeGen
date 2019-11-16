using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using CodeGenerator = Tocsoft.GraphQLCodeGen.CodeGenerator;

namespace Tocsoft.GraphQLCodeGen.Tests
{

    public class ClientOnlyDirectives
    {
        FakeLogger logger;
        CodeGeneratorSettingsLoader settingsLoader;
        public ClientOnlyDirectives()
        {
            logger = new FakeLogger();
            settingsLoader = new CodeGeneratorSettingsLoader(logger);
        }

        [Fact]
        public async Task ClientOnlyDirectivesShouldBeTrimedFromQueries()
        {
            var settings = settingsLoader.GenerateSettings(new CodeGeneratorSettingsLoaderDefaults(), new[] { "./Files/ClientOnlyDirectives/Query.gql" });

            CodeGenerator generator = new CodeGenerator(logger, settings.Single());

            await generator.LoadSource();
            generator.Parse();
            generator.Render();

            Assert.Empty(generator.Document.Errors);

            var code = generator.GeneratedCode;

            Assert.Contains(@"query q {
  test(id: """"safsa""""){
    nullable
    nonnullable
  }
}".Trim().Replace("\r", ""), code.Replace("\r", ""));
        }


        [Fact]
        public async Task TypeDirectiveCanChangeExportedTypeName()
        {
            var settings = settingsLoader.GenerateSettings(new CodeGeneratorSettingsLoaderDefaults(), new[] { "./Files/ClientOnlyDirectives/Query.gql" });

            CodeGenerator generator = new CodeGenerator(logger, settings.Single());

            await generator.LoadSource();
            generator.Parse();

            Assert.Empty(generator.Document.Errors);

            Assert.Single(generator.Model.Types.Select(x => x.Name), "TestResultABCD");
        }

        [Fact]
        public async Task ShouldRecieveErrorAboutTwoClassesWithSameName()
        {
            var settings = settingsLoader.GenerateSettings(new CodeGeneratorSettingsLoaderDefaults(), new[] { "./Files/ClientOnlyDirectives/QueryDuplicatedType.gql" });

            CodeGenerator generator = new CodeGenerator(logger, settings.Single());

            await generator.LoadSource();
            generator.Parse();

            var error = Assert.Single(generator.Document.Errors);
        }
    }
}
