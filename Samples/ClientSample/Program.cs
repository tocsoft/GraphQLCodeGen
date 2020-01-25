using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Sample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var epp = Client.TestClient.Episode.Empire;
            // lets create a simple console application that can sho github data

            //var accesstoken = args[0];

            //var httpClient = new HttpClient()
            //{
            //    BaseAddress = new Uri("https://api.github.com/graphql"),
            //    DefaultRequestHeaders = {
            //        { "User-Agent", "Tocsoft.GraphQLCodeGen.Sample" },
            //        { "Authorization", $"Bearer {accesstoken}" }
            //    }
            //};

            //httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            //var client = new Client.GitHub.GitHubClient(httpClient);
            //var result = await client.UsersRepositoresAsync("tocsoft", 10);
            //var repo = result.User.First.Nodes.First();
            //var id = repo.Id;

            ////var hasStared = await client.AddStarAsync(id);
            //var user = await client.CurrentUserAsync();
            //var b = user.Viewer.Bio;
        }
    }

    public class EnumClass
    {
        static Dictionary<string, EnumClass> lookup;

        public string Value { get; }
        public bool IsDefined { get; }

        private EnumClass(string value, bool isDefined)
        {
            this.Value = value;
            this.IsDefined = isDefined;
        }

        private static EnumClass Create(string value, bool isDefined)
        {
            if (lookup.TryGetValue(value, out var val))
            {
                return val;
            }

            lock (lookup)
            {
                if (lookup.TryGetValue(value, out val))
                {
                    return val;
                }

                val = new EnumClass(value, isDefined);
                lookup.Add(value, val);

                return val;
            }
        }

        public static implicit operator EnumClass(string value) => Create(value, false);
        public static implicit operator string(EnumClass value) => value.Value;

        public static EnumClass Value1 = EnumClass.Create("VALUE1", true);
        public static EnumClass Value2 = EnumClass.Create("VALUE2", true);
        public static EnumClass Value3 = EnumClass.Create("VALUE3", true);
    }
}
