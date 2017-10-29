using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Tocsoft.GraphQLCodeGen.MsBuild
{
    public class GenerateGraphQLClient : Task
    {
        //static CodeGeneratorSettingsLoader settingsLoader = new CodeGeneratorSettingsLoader();
        [Required]
        public string SettingPaths { get; set; }

        public string RootCliFolder { get; set; }

        [Required]
        public string IntermediateOutputDirectory { get; set; }

        [Output]
        public ITaskItem[] GeneratedCompile { get; set; }

        public override bool Execute()
        {
            var exeFolder = Path.GetDirectoryName(new Uri(typeof(GenerateGraphQLClient).GetTypeInfo().Assembly.Location).LocalPath);
            if (string.IsNullOrWhiteSpace(RootCliFolder))
            {
                RootCliFolder = Path.GetFullPath(Path.Combine(exeFolder, "..\\tool\\"));
            }
            var exePath = Path.Combine(RootCliFolder, "net46\\publish\\Tocsoft.GraphQLCodeGen.Cli.exe");

            var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var arguments = $"\"{SettingPaths}\" --msbuild-outputdir \"{Path.GetFullPath(tempFolder).TrimEnd(new[] { '\\', '/' })}\"";

            var realexe = exePath;
#if NETCOREAPP1_0
            realexe = "dotnet";
            arguments = $"\"{Path.Combine(RootCliFolder, "netcoreapp1.0\\publish\\Tocsoft.GraphQLCodeGen.Cli.dll")}\" {arguments}";
#endif
            Log.LogMessage(MessageImportance.Low, "Executing  \"{0}\" {1}", realexe, arguments);

            try
            {
                Directory.CreateDirectory(tempFolder);
                var process = Process.Start(new ProcessStartInfo(realexe, arguments)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                });
                process.WaitForExit(5000);
                if (!process.HasExited)
                {
                    process.Kill();
                    Log.LogMessage(MessageImportance.Low, "Executing  \"{0}\" {1} taking too long", realexe, arguments);

                    return false;
                }

                var filesoutput = process.StandardOutput.ReadToEnd();
                Log.LogMessage(MessageImportance.High, "generated = {0}", filesoutput);

                var files = filesoutput
                .Replace("\r", "")
                .Split('\n');

                var actualFiles = files.Where(x => !string.IsNullOrEmpty(x) && File.Exists(x));
                var fullIntermediateOutputDirectory = Path.GetFullPath(Path.Combine(IntermediateOutputDirectory,"GraphQLCodeGen"));
                Directory.CreateDirectory(fullIntermediateOutputDirectory);
                var sha = SHA256.Create();
                List<TaskItem> generateFiles = new List<TaskItem>();
                foreach (var f in actualFiles)
                {
                    var fileName = Path.GetFileName(f);
                    var newFileContent = File.ReadAllText(f);
                    var hashFile = Path.Combine(fullIntermediateOutputDirectory, fileName + ".hash");
                    var filedirty = true;
                    var actualHash = GetSha256Hash(sha, newFileContent); // TODO skip section while hashing (generated date etc)
                    if (File.Exists(hashFile))
                    {
                        var hash = File.ReadAllText(hashFile);
                        filedirty = hash != actualHash;
                    }
                    File.WriteAllText(hashFile, actualHash);

                    var targetPAth = Path.Combine(fullIntermediateOutputDirectory, fileName);
                    if (filedirty)
                    {
                        File.WriteAllText(targetPAth, newFileContent);
                    }
                    generateFiles.Add(new TaskItem(targetPAth));
                }
                // find matching files in this folder if they have identical hashes then skip them 

                // lets create a cache of files to makesure we don't keep overwriteing the same fiel on disk mutiple times and causing a rebuild

                GeneratedCompile = generateFiles.ToArray();

                return true;
            }
            finally
            {
               Directory.Delete(tempFolder, true);
            }
        }

        static string GetSha256Hash(SHA256 shaHash, string input)
        {
            // Convert the input string to a byte array and compute the hash.
            byte[] data = shaHash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
    }
}
