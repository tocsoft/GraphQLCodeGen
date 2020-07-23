using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Tocsoft.GraphQLCodeGen.Tests
{
    internal class Compiler
    {
        public Assembly Compile(params string[] sourceCode)
        {
            var ms = CompileBytes(sourceCode);

            if (ms == null)
            {
                throw new Exception("Failed to compile");
            }

            return AssemblyLoadContext.Default.LoadFromStream(ms);
        }

        private MemoryStream CompileBytes(params string[] sourceCode)
        {
            var peStream = new MemoryStream();
            var result = GenerateCode(sourceCode).Emit(peStream);

            if (!result.Success)
            {
                var failures = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

                foreach (var diagnostic in failures)
                {
                    Assert.True(false, diagnostic.ToString());
                }

                return null;
            }

            peStream.Seek(0, SeekOrigin.Begin);

            return peStream;
        }

        private static CSharpCompilation GenerateCode(string[] sourceCode)
        {
            var codeStrings = sourceCode.Select(x => SourceText.From(x));
            var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);

            var parsedSyntaxTree = codeStrings.Select(x => SyntaxFactory.ParseSyntaxTree(x, options));
            var netstandard = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName("netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51"));
            var references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(netstandard.Location),
                MetadataReference.CreateFromFile(netstandard.Location),
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Uri).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Net.Http.HttpClient).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Newtonsoft.Json.JsonWriter).Assembly.Location),
                
                MetadataReference.CreateFromFile(typeof(ReadOnlySpan<byte>).Assembly.Location),                
                MetadataReference.CreateFromFile(typeof(ReadOnlySpan<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Text.Json.JsonSerializer).Assembly.Location),

                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo).Assembly.Location),
            };

            return CSharpCompilation.Create($"Hello{Guid.NewGuid()}.dll",
                parsedSyntaxTree,
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default));
        }
    }
}
