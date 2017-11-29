using System;
using System.Linq;
using Xunit;

namespace Tocsoft.GraphQLCodeGen.Tests
{
    public class LoadSettingsFromHeaders
    {
        CodeGeneratorSettingsLoader settingsLoader = new CodeGeneratorSettingsLoader();

        [Fact]
        public void GenerateOuputSetsBasedOnCommonConfigsInSourceFiles()
        {
            var paths = "./Files/Test1/*.gql";

            var settings = settingsLoader.GenerateSettings(new[] { paths });

            Assert.Equal(2, settings.Count());
        }
    }
}
