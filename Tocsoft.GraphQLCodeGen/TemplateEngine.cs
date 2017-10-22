using System;
using System.Collections.Generic;
using System.Text;
using HandlebarsDotNet;
using System.IO;
using System.Reflection;
using System.Linq;

namespace Tocsoft.GraphQLCodeGen
{
    public class TemplateEngine
    {
        private IHandlebars engine;

        public TemplateEngine(string templateName)
        {

            this.engine = HandlebarsDotNet.Handlebars.Create(new HandlebarsConfiguration
            {
                ThrowOnUnresolvedBindingExpression = true
            });

            engine.RegisterHelper("concat", (writer, context, args) =>
            {
                writer.WriteSafeString(string.Concat(args));
            });

            engine.RegisterHelper("pascalCase", (writer, context, args) =>
            {
                writer.WriteSafeString(args[0].ToString().ToPascalCase());
            });

            engine.RegisterHelper("camelCase", (writer, context, args) =>
            {
                writer.WriteSafeString(args[0].ToString().ToCamelCase());
            });

            engine.RegisterHelper("replace", (writer, context, args) =>
            {
                var toReplace = args[1].ToString();
                var toReplaceWith = args[2].ToString();
                
                writer.WriteSafeString(args[0].ToString().Replace(toReplace, toReplaceWith));
            });

            engine.Configuration.TextEncoder = new NullEncoder();
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

            ProcessTemplate(LoadTemplate(templateName));
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
            return template.Invoke(model);
        }


        private void ProcessTemplate(string template)
        {

            StringReader reader = new StringReader(template);
            StringBuilder sb = new StringBuilder();
            string currentTemplateName = null;
            while (reader.Peek() >= 0)
            {
                var line = reader.ReadLine();
                var trimmedLine = line.TrimStart();
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
                this.template = engine.Compile(sb.ToString().Trim());
            }
            else
            {
                engine.RegisterTemplate(currentTemplateName, sb.ToString().TrimEnd());
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
                var typeinfo = typeof(CodeGenerator).GetTypeInfo();
                var resourceName = "Templates." + temp;

                var realName = typeinfo.Assembly.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase));
                if (realName != null)
                {
                    using (var s = new StreamReader(typeinfo.Assembly.GetManifestResourceStream(realName)))
                    {
                        return s.ReadToEnd();
                    }
                }
                return null;
            }

            var altName = template + ".template";
            return LoadTemplateDisk(template) ??
                LoadTemplateDisk(altName) ??
                LoadTemplateResource(template) ??
                LoadTemplateResource(altName);
        }
    }
}

