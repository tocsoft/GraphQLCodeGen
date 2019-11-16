using System.Collections.Generic;

namespace Tocsoft.GraphQLCodeGen.Models
{
    public class OperationViewModel
    {
        public string Name { get; set; }
        public TypeReferenceModel ResultType { get; internal set; }
        public List<NamedTypeViewModel> Arguments { get; internal set; }
        public string Query { get; internal set; }
        public string QueryFragment { get; internal set; }
        public string NamedQuery { get; internal set; }

        public OperationViewModel()
        {

        }
    }
}
