namespace Tocsoft.GraphQLCodeGen.Models
{
    public class TypeReferenceModel
    {
        public string TypeName { get; set; }
        public bool CanValueBeNull { get; set; }
        public bool IsCollection { get; set; }
        public bool CanCollectionBeNull { get; set; }
        public bool IsScaler { get; set; }
        public bool IsEnum { get; set; }
    }
}
