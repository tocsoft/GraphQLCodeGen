using GraphQL.Conventions;
using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQLApiSample
{
    [GraphQLQuery]
    public class GqlQuery
    {
        public NonNull<GqlQueryUser> Viewer
        {
            get => new GqlQueryUser("name here");
        }
    }
    public class GqlQueryUser
    {
        private string value;

        public GqlQueryUser(string value)
        {
            this.value = value;
        }
        public string Name => value;
    }
}
