using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using CodeGenerator = Tocsoft.GraphQLCodeGen.CodeGenerator;

namespace Tocsoft.GraphQLCodeGen.Tests
{
    public class Issue_9
    {
        FakeLogger logger;
        CodeGeneratorSettingsLoader settingsLoader;
        public Issue_9()
        {
            logger = new FakeLogger();
            settingsLoader = new CodeGeneratorSettingsLoader(logger);
        }

        [Fact]
        public async Task NullableFielsShouldGenerateNullableMetadata()
        {
            var settings = settingsLoader.GenerateSettings(new[] { "./Files/Issue_9/Query.gql" });

            CodeGenerator generator = new CodeGenerator(logger, settings.Single());

            await generator.LoadSource();
            generator.Parse();

            var model = generator.Model;

            var droidType = Assert.Single(model.Types.Where(x => x.Name == "DroidResult"));

            var nullableField = Assert.Single(droidType.Fields.Where(x => x.Name == "nullable"));
            var nonnullableField = Assert.Single(droidType.Fields.Where(x => x.Name == "nonnullable"));

            Assert.False(nonnullableField.Type.CanValueBeNull);
            Assert.True(nullableField.Type.CanValueBeNull);
        }

        [Fact]
        public async Task ShouldExportNullableField()
        {
            var settings = settingsLoader.GenerateSettings(new[] { "./Files/Issue_9/Query.gql" });

            CodeGenerator generator = new CodeGenerator(logger, settings.Single());

            await generator.LoadSource();
            generator.Parse();
            generator.Render();

            var code = generator.GeneratedCode;
            Assert.Contains("DateTime? Nullable { get; ", code);
            Assert.Contains("DateTime Nonnullable { get; ", code);
        }


        [Fact]
        public async Task NullableFielsShouldGenerateNullableMetadata_Gitub()
        {
            var settings = settingsLoader.GenerateSettings(new[] { "./Files/Issue_9/QueryGithub.gql" });

            CodeGenerator generator = new CodeGenerator(logger, settings.Single());

            await generator.LoadSource();
            generator.Parse();

            var model = generator.Model;

            var droidType = Assert.Single(model.Types.Where(x => x.Name == "RepositoryResult"));

            var databaseIdField = Assert.Single(droidType.Fields.Where(x => x.Name == "databaseId"));
            var createdAtField = Assert.Single(droidType.Fields.Where(x => x.Name == "createdAt"));
            
            Assert.True(databaseIdField.Type.CanValueBeNull);
            Assert.False(createdAtField.Type.CanValueBeNull);
        }

        [Fact]
        public async Task ShouldExportNullableField_Gitub()
        {
            var settings = settingsLoader.GenerateSettings(new[] { "./Files/Issue_9/QueryGithub.gql" });

            CodeGenerator generator = new CodeGenerator(logger, settings.Single());

            await generator.LoadSource();
            generator.Parse();
            generator.Render();

            var code = generator.GeneratedCode;
            Assert.Contains("int? DatabaseId { get; ", code);
            Assert.Contains("DateTime CreatedAt { get; ", code);
        }
    }
}
