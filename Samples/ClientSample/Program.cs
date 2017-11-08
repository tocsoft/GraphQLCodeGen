using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Sample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // lets create a simple console application that can sho github data

            var accesstoken = args[0];

            var httpClient = new HttpClient()
            {
                BaseAddress = new Uri("https://api.github.com/graphql"),
                DefaultRequestHeaders = {
                    { "User-Agent", "Tocsoft.GraphQLCodeGen.Sample" },
                    { "Authorization", $"Bearer {accesstoken}" }
                }
            };

            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));


            var client = new Client.GitHub.GitHubClient(httpClient);
            var result = await client.UsersRepositoresAsync("tocsoft", 10);
            var repo = result.User.First.Nodes.First();
            var id = repo.Id;

            //var hasStared = await client.AddStarAsync(id);
            var user = await client.CurrentUserAsync();
            var b = user.Viewer.Bio;
        }
    }
}
