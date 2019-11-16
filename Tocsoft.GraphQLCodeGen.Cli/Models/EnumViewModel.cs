using System.Collections.Generic;

namespace Tocsoft.GraphQLCodeGen.Models
{
    public class EnumViewModel
    {
        public EnumViewModel(string name)
        {
            // typename register needed to ensure types don't clash
            this.Name = name;
        }

        // a type is a list of fields and and unique name
        public string Name { get; set; }
        public IEnumerable<string> Values { get; set; }
    }
}
