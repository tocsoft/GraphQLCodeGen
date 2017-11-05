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
        public string Namespace { get; private set; }
        public string ClassName { get; private set; }

        private Dictionary<string, TypeViewModel> typeLookup = new Dictionary<string, TypeViewModel>();
        private Dictionary<IGraphQLType, string> inputTypeLookup = new Dictionary<IGraphQLType, string>();
        private List<TypeViewModel> typeCollection = new List<TypeViewModel>();
        private List<EnumViewModel> enumCollection = new List<EnumViewModel>();
        private List<OperationViewModel> operationCollection;

        public IEnumerable<TypeViewModel> Types => typeCollection;
        public IEnumerable<EnumViewModel> Enums => enumCollection;
        public IEnumerable<OperationViewModel> Operations => operationCollection;

        internal ViewModel(GraphQLDocument query, CodeGeneratorSettings settings)
        {
            Namespace = settings.Namespace;
            ClassName = settings.ClassName;

            this.typeLookup = new Dictionary<string, TypeViewModel>();
            this.inputTypeLookup = new Dictionary<IGraphQLType, string>();
            this.typeCollection = new List<TypeViewModel>();
            // lets name each set

            this.operationCollection = new List<OperationViewModel>();
            foreach (var op in query.Operations)
            {

                List<NamedTypeViewModel> argCollection = new List<NamedTypeViewModel>();
                foreach (var arg in op.Paramaters)
                {
                    argCollection.Add(new NamedTypeViewModel
                    {
                        Name = arg.Key,
                        Type = GenerateTypeReference(arg.Value)
                    });
                }

                var name = Path.GetFileNameWithoutExtension(op.Path);
                var opName = $"{name}_{op.Name }".Trim('_');
                var opVM = new OperationViewModel()
                {
                    Name = opName,
                    Arguments = argCollection,
                    Query = op.Query,
                    NamedQuery = op.Name ?? string.Empty,
                    ResultType = new TypeReferenceModel
                    {
                        IsCollection = false,
                        CanCollectionBeNull = false,
                        CanValueBeNull = false,
                        TypeName = GenerateType(op.Selection, opName)
                    }
                };

                operationCollection.Add(opVM);
            }

            // lets convert each to a type with a back track to lookup a type based on unique reference

            // I need type definitions for Operation Result and Argument types graphs
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

        private string GenerateType(SetSelection selection, string operationName = null)
        {
            if (typeLookup.ContainsKey($"{operationName}_{selection.UniqueIdentifier}"))
            {
                return typeLookup[$"{operationName}_{selection.UniqueIdentifier}"]?.Name;
            }

            var name = FindBestName(operationName ?? selection.RootType.Name, "Result");

            var type = new TypeViewModel(name)
            {
                Fields = selection.Fields.Select(x => new NamedTypeViewModel()
                {
                    Name = x.Name,
                    Type = GenerateTypeReference(x)
                }).ToList()
            };
            typeLookup.Add($"{operationName}_{selection.UniqueIdentifier}", type);
            typeCollection.Add(type);

            return type.Name;
        }

        private TypeReferenceModel GenerateTypeReference(ObjectModel.ValueTypeReference type)
        {
            var result = new TypeReferenceModel();
            if (type.Type is ScalarType ts)
            {
                result.IsScaler = true;
                result.TypeName = ts.Name;
            }
            else
            {
                result.TypeName = GenerateType(type.Type);
                result.IsScaler = false;
            }

            result.CanCollectionBeNull = type.CanCollectionBeNull;
            result.IsCollection = type.IsCollection;
            result.CanValueBeNull = type.CanValueBeNull;
            return result;
        }

        private string GenerateType(IGraphQLType type)
        {
            if (inputTypeLookup.ContainsKey(type))
            {
                return inputTypeLookup[type];
            }

            //lets make sure it exists
            var name = FindBestName(type.Name, "");

            if (type is IGraphQLFieldCollection obj)
            {

                var typeVm = new TypeViewModel(name)
                {
                    Fields = obj.Fields.Select(x =>
                         new NamedTypeViewModel()
                         {
                             Name = x.Name,
                             Type = GenerateTypeReference(x.Type)

                         }).ToList()
                };

                inputTypeLookup.Add(type, typeVm.Name);
                typeCollection.Add(typeVm);

                return typeVm.Name;
            }
            else if (type is EnumType enumObj)
            {
                var typeVm = new EnumViewModel(name)
                {
                    Values = enumObj.Values
                };
                inputTypeLookup.Add(type, typeVm.Name);
                enumCollection.Add(typeVm);
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
            var fieldName = $"{sourceName}{postFix}";
            int count = 0;
            while (typeCollection.Any(x => x.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase)))
            {
                count++;
                fieldName = $"{sourceName}{count}{postFix}";
            }
            return fieldName;
        }
    }

    public class OperationViewModel
    {
        public string Name { get; set; }
        public TypeReferenceModel ResultType { get; internal set; }
        public List<NamedTypeViewModel> Arguments { get; internal set; }
        public string Query { get; internal set; }
        public string NamedQuery { get; internal set; }

        public OperationViewModel()
        {

        }
    }


    public class NamedTypeViewModel
    {
        //arguent & field are the same
        public string Name { get; set; }
        public TypeReferenceModel Type { get; set; }
    }

    public class EnumViewModel
    {
        public EnumViewModel(string name)
        {
            // typename register needed to ensure types don't clash
            Name = name;
        }

        // a type is a list of fields and and unique name
        public string Name { get; set; }
        public IEnumerable<string> Values { get; set; }
    }

    public class TypeViewModel
    {
        // do we need to support interface or union types in the client, don't think we do
        public TypeViewModel(string name)
        {
            // typename register needed to ensure types don't clash
            Name = name;
        }

        //public IEnumerable<TypeViewModel> InheritsFrom { get; set; }
        //public bool IsInterface { get; set; }

        // a type is a list of fields and and unique name
        public string Name { get; set; }
        public IEnumerable<NamedTypeViewModel> Fields { get; set; }
    }


    public class TypeReferenceModel
    {
        public string TypeName { get; set; }
        public bool CanValueBeNull { get; set; }
        public bool IsCollection { get; set; }
        public bool CanCollectionBeNull { get; set; }
        public bool IsScaler { get; set; }
    }
}
