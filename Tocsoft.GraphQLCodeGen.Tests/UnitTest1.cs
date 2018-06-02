using System;
using System.Linq;
using Tocsoft.GraphQLCodeGen.Cli;
using Xunit;

namespace Tocsoft.GraphQLCodeGen.Tests
{
    public class LoadSettingsFromHeaders
    {

        CodeGeneratorSettingsLoader settingsLoader = new CodeGeneratorSettingsLoader(new FakeLogger());

        [Fact]
        public void GenerateOuputSetsBasedOnCommonConfigsInSourceFiles()
        {
            var paths = "./Files/Test1/*.gql";

            var settings = settingsLoader.GenerateSettings(new CodeGeneratorSettingsLoaderDefaults(), new[] { paths });

            Assert.Equal(2, settings.Count());
        }
    }

    public class FakeLogger : ILogger
    {
        public void Error(string str)
        {
        }

        public void Message(string str)
        {
        }
    }
}
