using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using CodeGenerator = Tocsoft.GraphQLCodeGen.CodeGenerator;

namespace Tocsoft.GraphQLCodeGen.Tests
{
    public class Issue_8
    {
        FakeLogger logger;
        CodeGeneratorSettingsLoader settingsLoader;
        public Issue_8()
        {
            logger = new FakeLogger();
            settingsLoader = new CodeGeneratorSettingsLoader(logger);
        }

        [Fact]
        public async Task TypeNamesShouldBeUnique()
        {
            var settings = settingsLoader.GenerateSettings(new CodeGeneratorSettingsLoaderDefaults(), new[] { "./Files/Issue_8/Quote.gql" });

            CodeGenerator generator = new CodeGenerator(logger, settings.Single());

            await generator.LoadSource();
            generator.Parse();

            var model = generator.Model;

            var resultType = Assert.Single(model.Types.Where(x => x.Name == "QuoteResult"));
        }
    }
}
