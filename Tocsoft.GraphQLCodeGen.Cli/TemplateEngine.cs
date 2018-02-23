using System;
using System.Collections.Generic;
using System.Text;
using HandlebarsDotNet;
using System.IO;
using System.Reflection;
using System.Linq;
using Tocsoft.GraphQLCodeGen.Cli;
using System.Text.RegularExpressions;

namespace Tocsoft.GraphQLCodeGen
{
    public class TemplateEngine
    {
        private readonly ILogger logger;
        private IHandlebars engine;

        public TemplateEngine(IEnumerable<string> templates, ILogger logger)
        {
            this.logger = logger;
            this.engine = HandlebarsDotNet.Handlebars.Create(new HandlebarsConfiguration
            {
                ThrowOnUnresolvedBindingExpression = true
            });

            this.engine.RegisterHelper("concat", (writer, context, args) =>
            {
                writer.WriteSafeString(string.Concat(args));
            });

            this.engine.RegisterHelper("pascalCase", (writer, context, args) =>
            {
                writer.WriteSafeString(args[0].ToString().ToPascalCase());
            });

            this.engine.RegisterHelper("camelCase", (writer, context, args) =>
            {
                writer.WriteSafeString(args[0].ToString().ToCamelCase());
            });

            this.engine.RegisterHelper("replace", (writer, context, args) =>
            {
                string toReplace = args[1].ToString();
                string toReplaceWith = args[2].ToString();

                writer.WriteSafeString(args[0].ToString().Replace(toReplace, toReplaceWith));
            });

            this.engine.Configuration.TextEncoder = new NullEncoder();
            //engine.RegisterHelper("render", (w, c, a) =>
            //{
            //    if (c is Models.TypeViewModel typeVm)
            //    {
            //        // this is the typerefcontext
            //        w.WriteSafeString("Type");
            //        return;
            //    }
            //    if (c is Models.TypeReferenceModel typeRef)
            //    {
            //        // this is the typerefcontext
            //        w.WriteSafeString("TypeReference_" + typeRef.ScalerType.ToString());
            //        return;
            //    }
            //});

            foreach (string templatePath in templates)
            {
                string templateContents = LoadTemplate(templatePath);
                ProcessTemplate(templateContents);
            }
        }

        private class NullEncoder : ITextEncoder
        {
            public string Encode(string value)
            {
                return value;
            }
        }

        private Func<object, string> template;

        public string Generate(object model)
        {
            try
            {
                Func<object, string> template = this.engine.Compile("{{> Main}}");
                return template.Invoke(model);
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
                //throw ex;
                return "";
            }
        }


        private void ProcessTemplate(string template)
        {

            StringReader reader = new StringReader(template);
            StringBuilder sb = new StringBuilder();
            string currentTemplateName = null;
            while (reader.Peek() >= 0)
            {
                string line = reader.ReadLine();
                string trimmedLine = line.TrimStart();
                if (trimmedLine.StartsWith("{{!#")) // we have a shebang
                {
                    RegisterTemplate(sb, currentTemplateName);

                    currentTemplateName = trimmedLine.Substring(4).Trim().Trim(new[] { '{', '}' }).Trim();
                }
                else
                {
                    // we have a slot to populate
                    sb.AppendLine(line);
                }
            }

            //we reached the end of the document
            RegisterTemplate(sb, currentTemplateName);
        }

        private void RegisterTemplate(StringBuilder sb, string currentTemplateName)
        {
            if (currentTemplateName == null)
            {
                this.template = this.engine.Compile(sb.ToString().Trim());
            }
            else
            {
                this.engine.RegisterTemplate(currentTemplateName, sb.ToString().TrimEnd());
            }
            sb.Clear();
        }



        public static string LoadTemplate(string template)
        {
            string LoadTemplateDisk(string temp)
            {
                // file on disk wins
                if (File.Exists(temp))
                {
                    return File.ReadAllText(temp);
                }
                return null;
            }
            string LoadTemplateResource(string temp)
            {
                TypeInfo typeinfo = typeof(CodeGenerator).GetTypeInfo();

                if (typeinfo.Assembly.GetManifestResourceNames().Contains(temp))
                {
                    using (StreamReader s = new StreamReader(typeinfo.Assembly.GetManifestResourceStream(temp)))
                    {
                        return s.ReadToEnd();
                    }
                }
                return null;
            }

            return LoadTemplateDisk(template) ??
                LoadTemplateResource(template);
        }
    }
}

