using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Tocsoft.GraphQLCodeGen.Tests
{
    public class NullableReferenceTypes
    {
        private readonly CodeGeneratorTester tester;

        public NullableReferenceTypes()
        {
            tester = new CodeGeneratorTester();
        }

        [Fact]
        public async Task NullableWithValue_Enabled()
        {
            tester.AddQuery("./Files/NullableReferenceTypes/NullableWithValue.gql");
            tester.Configure(s =>
            {
                s.TemplateSettings["Nullable"] = "enabled";
            });

            var code = await tester.Generate();
            await tester.Verify();

            Assert.Contains("public Test.DroidResult? Test { get; set; }", code);
            Assert.Contains("Task<Test.NullableWithValueQResult> NullableWithValueQAsync(string? temp);", code);
        }
        [Fact]
        public async Task NonNullableWithValue_Enabled()
        {
            tester.AddQuery("./Files/NullableReferenceTypes/NonNullableWithValue.gql");
            tester.Configure(s =>
            {
                s.TemplateSettings["Nullable"] = "enabled";
            });

            var code = await tester.Generate();
            await tester.Verify();
            Assert.Contains("public Test.DroidResult TestNonNull { get; set; }", code);
            Assert.Contains("Task<Test.NonNullableWithValueQResult> NonNullableWithValueQAsync(string temp);", code);
        }

        [Fact]
        public async Task NullableWithValue_Disabled()
        {
            tester.AddQuery("./Files/NullableReferenceTypes/NullableWithValue.gql");
            tester.Configure(s =>
            {
                s.TemplateSettings["Nullable"] = "disabled";
            });

            var code = await tester.Generate();
            await tester.Verify();
            Assert.Contains("public Test.DroidResult TestNonNull { get; set; }", code);
            Assert.Contains("Task<Test.NullableWithValueQResult> NullableWithValueQAsync(string temp);", code);
        }

        [Fact]
        public async Task NonNullableWithValue_Disabled()
        {
            tester.AddQuery("./Files/NullableReferenceTypes/NonNullableWithValue.gql");
            tester.Configure(s =>
            {
                s.TemplateSettings["Nullable"] = "disabled";
            });

            var code = await tester.Generate();
            await tester.Verify();
            Assert.Contains("public Test.DroidResult TestNonNull { get; set; }", code);
            Assert.Contains("Task<Test.NonNullableWithValueQResult> NonNullableWithValueQAsync(string temp);", code);
        }
    }
}
