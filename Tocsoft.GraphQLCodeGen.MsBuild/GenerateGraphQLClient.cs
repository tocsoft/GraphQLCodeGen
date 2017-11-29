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
        [Required]
        public ITaskItem[] Content { get; set; }
        [Required]
        public ITaskItem[] None { get; set; }

        public string RootCliFolder { get; set; }

        [Required]
        public string IntermediateOutputDirectory { get; set; }

        [Output]
        public ITaskItem[] GeneratedCompile { get; set; }

        public override bool Execute()
        {
            // list all source settings files
            // upldate this to include *.gql/*.graphql if/when i update it to support metadata in config file
            IEnumerable<ITaskItem> settings =
                this.None.Union(this.Content)
                .Where(x => x.GetMetadata("Generator") == "MSBuild:GenerateGraphQLClient")
                .Where(x => x.ItemSpec.EndsWith(".gql") || x.ItemSpec.EndsWith(".graphql"));

            string exeFolder = Path.GetDirectoryName(new Uri(typeof(GenerateGraphQLClient).GetTypeInfo().Assembly.Location).LocalPath);
            if (string.IsNullOrWhiteSpace(this.RootCliFolder))
            {
                this.RootCliFolder = Path.GetFullPath(Path.Combine(exeFolder, "..\\binaries\\"));
            }

            // has fullframewrok
            bool fullFramework = false;
            string windir = Environment.GetEnvironmentVariable("windir");
            if (!string.IsNullOrWhiteSpace(windir))
            {
                if (Directory.Exists(Path.Combine(windir, "Microsoft.NET")))
                {
                    fullFramework = true;
                }
            }

            string exePath = Path.Combine(this.RootCliFolder, "net461\\Tocsoft.GraphQLCodeGen.Cli.exe");

            string tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            string settingArgs = string.Join(" ", settings.Select(x => $"\"{Path.GetFullPath(x.ItemSpec)}\""));
            string arguments = $"{settingArgs} --msbuild-outputdir \"{Path.GetFullPath(tempFolder).TrimEnd(new[] { '\\', '/' })}\"";

            string realexe = exePath;
            if (!fullFramework)
            {
                realexe = "dotnet";
                arguments = $"\"{Path.Combine(this.RootCliFolder, "netcoreapp1.0\\Tocsoft.GraphQLCodeGen.Cli.dll")}\" {arguments}";
            }

            this.Log.LogMessage(MessageImportance.Low, "Executing  \"{0}\" {1}", realexe, arguments);

            try
            {
                Directory.CreateDirectory(tempFolder);
                Process process = Process.Start(new ProcessStartInfo(realexe, arguments)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                });
                process.WaitForExit(5000);
                if (!process.HasExited)
                {
                    process.Kill();
                    this.Log.LogMessage(MessageImportance.Low, "Executing  \"{0}\" {1} taking too long", realexe, arguments);

                    return false;
                }

                string filesoutput = process.StandardOutput.ReadToEnd();
                this.Log.LogMessage(MessageImportance.High, "generated = {0}", filesoutput);

                IEnumerable<string> files = filesoutput
                .Replace("\r", "")
                .Split('\n')
                .Where(x=>x.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)); // only fiddle with csharp files

                IEnumerable<string> actualFiles = files.Where(x => !string.IsNullOrEmpty(x) && File.Exists(x));
                string fullIntermediateOutputDirectory = Path.GetFullPath(Path.Combine(this.IntermediateOutputDirectory, "GraphQLCodeGen"));
                Directory.CreateDirectory(fullIntermediateOutputDirectory);
                SHA256 sha = SHA256.Create();
                List<TaskItem> generateFiles = new List<TaskItem>();
                foreach (string f in actualFiles)
                {
                    string fileName = Path.GetFileName(f);
                    string newFileContent = File.ReadAllText(f);
                    string hashFile = Path.Combine(fullIntermediateOutputDirectory, fileName + ".hash");
                    bool filedirty = true;
                    string actualHash = GetSha256Hash(sha, newFileContent); // TODO skip section while hashing (generated date etc)
                    if (File.Exists(hashFile))
                    {
                        string hash = File.ReadAllText(hashFile);
                        filedirty = hash != actualHash;
                    }
                    File.WriteAllText(hashFile, actualHash);

                    string targetPAth = Path.Combine(fullIntermediateOutputDirectory, fileName);
                    if (filedirty)
                    {
                        File.WriteAllText(targetPAth, newFileContent);
                    }
                    generateFiles.Add(new TaskItem(targetPAth));
                }
                // find matching files in this folder if they have identical hashes then skip them 

                // lets create a cache of files to makesure we don't keep overwriteing the same fiel on disk mutiple times and causing a rebuild

                this.GeneratedCompile = generateFiles.ToArray();

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
