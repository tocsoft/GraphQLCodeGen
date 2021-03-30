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
using System.Text.RegularExpressions;

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

        [Required]
        public string Timeout { get; set; }

        public string SettingsPath { get; set; }

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

            string exePath = Path.GetFullPath(Path.Combine(this.RootCliFolder, "net461\\Tocsoft.GraphQLCodeGen.Cli.exe").Replace('\\', Path.DirectorySeparatorChar));

            string tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            string settingArgs = string.Join(" ", settings.Select(x => Path.GetFullPath(x.ItemSpec).EscapeAndQuotePathArgument()));
            string arguments = $"{settingArgs} --msbuild-outputdir {Path.GetFullPath(tempFolder).EscapeAndQuotePathArgument()}";

            if (!string.IsNullOrWhiteSpace(SettingsPath))
            {
                arguments = $"{arguments} --settings {Path.GetFullPath(SettingsPath).EscapeAndQuotePathArgument()}";
            }

            string realexe = exePath;
            if (!fullFramework)
            {
                var dllPath = Path.GetFullPath(Path.Combine(this.RootCliFolder, "netcoreapp3.1\\Tocsoft.GraphQLCodeGen.Cli.dll").Replace('\\', Path.DirectorySeparatorChar));

                realexe = "dotnet";
                arguments = $"\"{dllPath}\" {arguments}";
            }

            this.Log.LogMessage(MessageImportance.Low, "Executing  \"{0}\" {1}", realexe, arguments);

            try
            {
                Directory.CreateDirectory(tempFolder);
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo(realexe, arguments)
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    },
                    EnableRaisingEvents = true
                };

                // we have to consume the output buffers otherwise they fill up
                // and the child process hangs.
                StringBuilder standardout = new StringBuilder();
                StringBuilder errorout = new StringBuilder();
                process.OutputDataReceived += (s, e) =>
                {
                    standardout.AppendLine(e.Data);
                };
                process.ErrorDataReceived += (s, e) =>
                {
                    errorout.AppendLine(e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // default timeout of 5 seconds
                int timeout = 5000;
                if (!int.TryParse(this.Timeout, out timeout))
                {
                    timeout = 5000;
                }

                // min timeout of 5 seconds
                if (timeout < 5000)
                {
                    timeout = 5000;
                }

                process.WaitForExit(timeout);
                if (!process.HasExited)
                {
                    process.Kill();
                    this.Log.LogError("Executing  \"{0}\" {1} taking too long", realexe, arguments);

                    return false;
                }

                var errors = errorout.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(errors))
                {
                    var errorLines = errors
                        .Replace("\r", "")
                        .Split('\n');
                    var regex = new Regex(@"(?:(.*?)\((\d+),(\d+)\): )?(ERROR) (.*?): (.*)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    foreach (var l in errorLines)
                    {
                        var res = regex.Match(l);
                        if (res.Success)
                        {
                            var path = res.Groups[1].Success ? res.Groups[1].Value : (string)null;
                            var line = int.Parse(res.Groups[2].Success ? res.Groups[2].Value : "0");
                            var col = int.Parse(res.Groups[3].Success ? res.Groups[3].Value : "0");
                            var code = res.Groups[5].Value;
                            var message = res.Groups[6].Value;
                            var level = res.Groups[4].Value;

                            if (level.Equals("error", StringComparison.OrdinalIgnoreCase))
                            {
                                this.Log.LogError(null, code, null, path, line, col, 0, 0, message, new object[] { });
                            }
                            else
                            {
                                this.Log.LogWarning(null, code, null, path, line, col, 0, 0, message, new object[] { });
                            }
                        }
                    }
                    // errors generated and written we should stop now
                    return false;
                }

                string filesoutput = standardout.ToString();
                this.Log.LogMessage(MessageImportance.High, "generated = {0}", filesoutput);

                IEnumerable<string> files = filesoutput
                .Replace("\r", "")
                .Split('\n')
                .Where(x => x.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)); // only fiddle with csharp files

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
                    string targetPath = Path.Combine(fullIntermediateOutputDirectory, fileName);

                    bool filedirty = true;
                    string actualHash = GetSha256Hash(sha, newFileContent); // TODO skip section while hashing (generated date etc)
                    if (File.Exists(hashFile) && File.Exists(targetPath)) // ensure we only try and skip if files exist on disk 
                    {
                        string hash = File.ReadAllText(hashFile);
                        filedirty = hash != actualHash;
                    }

                    if (filedirty)
                    {
                        File.WriteAllText(hashFile, actualHash);
                        File.WriteAllText(targetPath, newFileContent);
                    }
                    generateFiles.Add(new TaskItem(targetPath));
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

    internal static class Helpers
    {
        public static string EscapeAndQuoteArgument(this string txt)
        {
            return $"\"{txt.TrimEnd(new[] { '\\', '/' })}\"";
        }
        public static string EscapeAndQuotePathArgument(this string txt)
        {
            return txt.TrimEnd(new[] { '\\', '/' }).EscapeAndQuoteArgument(); ;
        }
    }
}
