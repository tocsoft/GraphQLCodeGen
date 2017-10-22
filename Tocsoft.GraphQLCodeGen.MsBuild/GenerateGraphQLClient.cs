using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Tocsoft.GraphQLCodeGen.MsBuild
{
    public class GenerateGraphQLClient : Task
    {
        static CodeGeneratorSettingsLoader settingsLoader = new CodeGeneratorSettingsLoader();
        [Required]
        public string SettingPaths { get; set; }

        [Required]
        public string IntermediateOutputDirectory { get; set; }

        [Output]
        public ITaskItem[] GeneratedCompile { get; set; }

        public override bool Execute()
        {
            Log.LogMessage("Generating Client Code from GraphQL Queries");

            var settings = settingsLoader.LoadFromPath(SettingPaths);

            List<ITaskItem> generatedFiles = new List<ITaskItem>();

            foreach (var s in settings)
            {
                Log.LogMessage("Generating GraphQL client '{0}.{1}' from '{2}'", s.Namespace, s.ClassName, s.SettingsPath);

                if (s.Template.Equals("csharp", StringComparison.OrdinalIgnoreCase))
                {
                    var dir = Path.Combine(IntermediateOutputDirectory, "GraphQLCodeGen");
                    Directory.CreateDirectory(dir) ;
                    // only applicable for csharp files!!!
                    s.OutputPath = Path.Combine(dir, $"{s.Namespace}.{s.ClassName}.cs");

                    generatedFiles.Add(new TaskItem(s.OutputPath));
                }

                var generator = new CodeGenerator(s);

                generator.Generate();
            }

            GeneratedCompile = generatedFiles.ToArray();

            return true;
        }
    }
}
