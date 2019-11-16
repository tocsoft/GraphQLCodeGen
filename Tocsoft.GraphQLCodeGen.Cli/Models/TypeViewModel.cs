using System.Collections.Generic;

namespace Tocsoft.GraphQLCodeGen.Models
{
    public class TypeViewModel
    {
        // do we need to support interface or union types in the client, don't think we do
        public TypeViewModel(string name)
        {
            // typename register needed to ensure types don't clash
            this.Name = name;
        }

        //public IEnumerable<TypeViewModel> InheritsFrom { get; set; }
        //public bool IsInterface { get; set; }

        // a type is a list of fields and and unique name
        public string Name { get; set; }

        public bool IsInterface { get; set; }

        public IEnumerable<NamedTypeViewModel> Fields { get; set; }

        public IEnumerable<string> Interfaces { get; set; }
    }
}
