using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static Tocsoft.GraphQLCodeGen.CodeGeneratorSettingsLoader;

namespace Tocsoft.GraphQLCodeGen.SchemaIntrospection
{
    public class HttpIntrospection : IIntrosepctionProvider
    {
        public const string IntrospectionQuery = @"
  query IntrospectionQuery {
    __schema {
      queryType { name }
      mutationType { name }
      subscriptionType { name }
      types {
        ...FullType
      }
      directives {
        name
        description
        locations
        args {
          ...InputValue
        }
      }
    }
  }
  fragment FullType on __Type {
    kind
    name
    description
    fields(includeDeprecated: true) {
      name
      description
      args {
        ...InputValue
      }
      type {
        ...TypeRef
      }
      isDeprecated
      deprecationReason
    }
    inputFields {
      ...InputValue
    }
    interfaces {
      ...TypeRef
    }
    enumValues(includeDeprecated: true) {
      name
      description
      isDeprecated
      deprecationReason
    }
    possibleTypes {
      ...TypeRef
    }
  }
  fragment InputValue on __InputValue {
    name
    description
    type { ...TypeRef }
    defaultValue
  }
  fragment TypeRef on __Type {
    kind
    name
    ofType {
      kind
      name
      ofType {
        kind
        name
        ofType {
          kind
          name
          ofType {
            kind
            name
            ofType {
              kind
              name
              ofType {
                kind
                name
                ofType {
                  kind
                  name
                }
              }
            }
          }
        }
      }
    }
  }
";
        public SchemaSource.SchemaTypes SchemaType => SchemaSource.SchemaTypes.Http;

        public async Task<string> LoadSchema(SchemaSource source)
        {
            var client = new HttpClient()
            {
                BaseAddress = new Uri(source.Location),
            };

            foreach (var h in source.Headers)
            {
                client.DefaultRequestHeaders.Add(h.Key, h.Value);
            }

            var response = await client.PostAsync(source.Location,
                new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    query = IntrospectionQuery
                }), Encoding.UTF8, "application/json"));

            var json = await response.Content.ReadAsStringAsync();

            return JsonIntrospection.ConvertJsonToSchema(json);
        }
    }
}
