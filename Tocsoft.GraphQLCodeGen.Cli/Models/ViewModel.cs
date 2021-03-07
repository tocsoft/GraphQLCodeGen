using HotChocolate.Language;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Tocsoft.GraphQLCodeGen.ObjectModel;
using Tocsoft.GraphQLCodeGen.ObjectModel.Selections;

namespace Tocsoft.GraphQLCodeGen.Models
{
    public class ViewModel
    {
        private readonly ObjectModel.GraphQLDocument query;

        public string Namespace { get; private set; }
        public string ClassName { get; private set; }
        public string OutputPath { get; }
        public string RootPath { get; }

        private Dictionary<string, TypeViewModel> typeLookup = new Dictionary<string, TypeViewModel>();
        private Dictionary<IGraphQLType, string> inputTypeLookup = new Dictionary<IGraphQLType, string>();
        private List<TypeViewModel> typeCollection = new List<TypeViewModel>();
        private List<EnumViewModel> enumCollection = new List<EnumViewModel>();
        private List<OperationViewModel> operationCollection;

        private Dictionary<string, string> fragmentQueries = new Dictionary<string, string>();

        public IEnumerable<TypeViewModel> Types => this.typeCollection;
        public IEnumerable<EnumViewModel> Enums => this.enumCollection;
        public IEnumerable<OperationViewModel> Operations => this.operationCollection;

        internal ViewModel(GraphQLDocument query, CodeGeneratorSettings settings)
        {
            this.query = query;
            this.Namespace = settings.Namespace;
            this.ClassName = settings.ClassName;
            this.OutputPath = settings.OutputPath;
            this.RootPath = settings.RootPath;

            this.typeLookup = new Dictionary<string, TypeViewModel>();
            this.inputTypeLookup = new Dictionary<IGraphQLType, string>();
            this.typeCollection = new List<TypeViewModel>();
            // lets name each set

            this.operationCollection = new List<OperationViewModel>();
            foreach (Operation op in query.Operations)
            {

                List<NamedTypeViewModel> argCollection = new List<NamedTypeViewModel>();
                foreach (KeyValuePair<string, ValueTypeReference> arg in op.Paramaters)
                {
                    argCollection.Add(new NamedTypeViewModel
                    {
                        Name = arg.Key,
                        Type = GenerateTypeReference(arg.Value)
                    });
                }

                string name = Path.GetFileNameWithoutExtension(op.Path);
                string opName = $"{name}_{op.Name }".Trim('_');
                OperationViewModel opVM = new OperationViewModel()
                {
                    Name = opName,
                    OperationName = op.Name ?? opName,
                    Arguments = argCollection,
                    QueryFragment = op.Query,
                    NamedQuery = op.Name ?? string.Empty,
                    ResultType = new TypeReferenceModel
                    {
                        IsCollection = false,
                        CanCollectionBeNull = false,
                        CanValueBeNull = false,
                        TypeName = GenerateType(op.Selection, opName)
                    }
                };
                // collect all fragments reference from this query and grab thier nodes query strings too
                opVM.Query = FullQuery(opVM);
                this.operationCollection.Add(opVM);
            }
        }

        private void AddError(string message, IGraphQLASTNodeLinked node)
        {
            this.query.AddError(ErrorCodes.Unknown, message, node.ASTNode);
        }
        private void AddError(string message, Location location)
        {
            this.query.AddError(ErrorCodes.Unknown, message, location);
        }

        private string FullQuery(OperationViewModel opVM)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(opVM.QueryFragment);
            var allTypes = ReferencedTypeList(opVM.ResultType.TypeName).Distinct();
            foreach (var t in allTypes)
            {
                if (this.fragmentQueries.ContainsKey(t))
                {
                    sb.AppendLine(this.fragmentQueries[t]);
                }
            }
            return sb.ToString();

            IEnumerable<string> ReferencedTypeList(string typeName)
            {
                var types = this.typeCollection.Where(x => x.Name == typeName);
                foreach (var t in types)
                {
                    foreach (var f in t.Interfaces)
                    {
                        yield return f;
                    }
                    foreach (var f in t.Fields)
                    {
                        var childTypes = ReferencedTypeList(f.Type.TypeName);
                        foreach (var ct in childTypes)
                        {
                            yield return ct;
                        }
                    }
                }
            }
        }

        private TypeReferenceModel GenerateTypeReference(FieldSelection field)
        {
            if (field.Selection == null) // this is a leaf thus can be a projection
            {
                return GenerateTypeReference(field.Type.Type);
            }
            else
            {
                return new TypeReferenceModel
                {
                    CanCollectionBeNull = field.Type.Type.CanCollectionBeNull,
                    IsCollection = field.Type.Type.IsCollection,
                    CanValueBeNull = field.Type.Type.CanValueBeNull,
                    TypeName = GenerateType(field.Selection)
                };
            }
        }

        private Dictionary<string, string> specifiedNameToUniqueIdentifierLookup = new Dictionary<string, string>();

        private string GenerateType(SetSelection selection, string operationName = null)
        {
            if (selection.SpecifiedTypeName is object)
            {
                if (specifiedNameToUniqueIdentifierLookup.TryGetValue(selection.SpecifiedTypeName.Value, out var key) && key != selection.UniqueIdentifier)
                {
                    this.AddError($"'{selection.SpecifiedTypeName.Value}' already defined with different fields", selection.SpecifiedTypeName.Location);
                }
            }

            var lookupName = $"{operationName}_{selection.SpecifiedTypeName?.Value ?? selection.UniqueIdentifier}";
            if (this.typeLookup.ContainsKey(lookupName))
            {
                return this.typeLookup[lookupName]?.Name;
            }

            string name = selection.SpecifiedTypeName?.Value ?? FindBestName((operationName ?? selection.RootType.Name), "Result");

            TypeViewModel type = BuildTypeViewModel(selection, name, lookupName);

            if (selection.SpecifiedTypeName is object)
            {
                specifiedNameToUniqueIdentifierLookup[selection.SpecifiedTypeName.Value] = selection.UniqueIdentifier;
            }

            return type.Name;
        }

        private TypeViewModel BuildTypeViewModel(SetSelection selection, string name, string lookupName)
        {
            TypeViewModel type = new TypeViewModel(name);

            this.typeLookup.Add(lookupName, type);
            this.typeCollection.Add(type);
            type.Fields = selection.Fields.Select(x => new NamedTypeViewModel()
            {
                Name = x.Name,
                Type = GenerateTypeReference(x)
            }).ToList();

            type.Interfaces = selection.Fragments.Select(x => GenerateType(x)).ToList();
            return type;
        }

        private TypeReferenceModel GenerateTypeReference(ObjectModel.ValueTypeReference type)
        {
            TypeReferenceModel result = new TypeReferenceModel();
            if (type.Type is ScalarType ts)
            {
                result.TypeName = ts.Name;
                result.IsScaler = true;
                result.IsEnum = false;
            }
            else if (type.Type is EnumType te)
            {
                result.TypeName = GenerateType(type.Type);
                result.IsScaler = false;
                result.IsEnum = true;
            }
            else
            {
                result.TypeName = GenerateType(type.Type);
                result.IsScaler = false;
                result.IsEnum = false;
            }

            result.CanCollectionBeNull = type.CanCollectionBeNull;
            result.IsCollection = type.IsCollection;
            result.CanValueBeNull = type.CanValueBeNull;
            return result;
        }

        private string GenerateType(IGraphQLType type)
        {
            if (this.inputTypeLookup.ContainsKey(type))
            {
                return this.inputTypeLookup[type];
            }

            //lets make sure it exists
            string name = FindBestName(type.Name, "");

            if (type is IGraphQLFieldCollection obj)
            {

                TypeViewModel typeVm = new TypeViewModel(name)
                {
                    Fields = obj.Fields.Select(x =>
                         new NamedTypeViewModel()
                         {
                             Name = x.Name,
                             Type = GenerateTypeReference(x.Type)

                         }).ToList()
                };

                this.inputTypeLookup.Add(type, typeVm.Name);
                this.typeCollection.Add(typeVm);

                return typeVm.Name;
            }
            else if (type is EnumType enumObj)
            {
                EnumViewModel typeVm = new EnumViewModel(name)
                {
                    Values = enumObj.Values
                };
                this.inputTypeLookup.Add(type, typeVm.Name);
                this.enumCollection.Add(typeVm);
                return typeVm.Name;
            }
            else if (type is FragmentType fragment)
            {

                TypeViewModel typeVm = BuildTypeViewModel(fragment.Selection, name, $"_{fragment.Selection}_[fragment]");
                typeVm.IsInterface = true;

                fragmentQueries.Add(typeVm.Name, fragment.Query);
                this.inputTypeLookup.Add(type, typeVm.Name);

                return typeVm.Name;
            }
            else
            {
                throw new Exception("unkown type");
                // probably an enum type
            }
        }


        private string FindBestName(string sourceName, string postFix)
        {
            string fieldName = $"{sourceName}{postFix}";
            int count = 0;
            while (this.typeCollection.Any(x => x.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase)))
            {
                count++;
                fieldName = $"{sourceName}{count}{postFix}";
            }
            return fieldName;
        }
    }
}
