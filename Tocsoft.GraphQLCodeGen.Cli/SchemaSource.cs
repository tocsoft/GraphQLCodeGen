using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tocsoft.GraphQLCodeGen
{
    public class SchemaSource
    {
        // we guess based on the string value what type of introspectino to do,
        // if its http/https then its a http introspection
        // if its a file path its eather a schema or json file
        // if its a dll then its a GraphQL conventions schema and other paramaters kick in
        public string Location { get; set; }

        /// <summary>
        /// for http based introspection then we use the headers
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// for dll based introspection this is the list of types that make up the queries
        /// </summary>
        public List<string> QueryType { get; set; } = new List<string>();

        /// <summary>
        /// for dll based introspection this is the list of types that make up the queries
        /// </summary>
        public List<string> MutationType { get; set; } = new List<string>();

        public SchemaTypes SchemaType()
        {
            if (this.Location.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return SchemaTypes.Http;
            }

            if (this.Location.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || this.Location.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                return SchemaTypes.Dll;
            }

            if (this.Location.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                return SchemaTypes.Json;
            }

            return SchemaTypes.GraphQLSchemaFile;
        }

        public enum SchemaTypes
        {
            Http,
            Dll,
            Json,
            GraphQLSchemaFile
        }

        internal string SettingsHash()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(Location);
            sb.Append("~#~");
            if (QueryType != null)
            {
                foreach (var t in QueryType)
                {
                    sb.Append(t);
                    sb.Append("~#~");
                }
            }
            if (MutationType != null)
            {
                foreach (var t in MutationType)
                {
                    sb.Append(t);
                    sb.Append("~#~");
                }
            }

            return sb.ToString();
        }
    }
}
