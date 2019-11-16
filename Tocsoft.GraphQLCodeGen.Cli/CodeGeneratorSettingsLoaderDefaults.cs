using System;

namespace Tocsoft.GraphQLCodeGen
{
    internal class CodeGeneratorSettingsLoaderDefaults
    {
        public string Format { get; set; }

        public string OutputPath { get; set; }

        public Action<SimpleSourceFile> FixFile { get; set; }
    }
}
