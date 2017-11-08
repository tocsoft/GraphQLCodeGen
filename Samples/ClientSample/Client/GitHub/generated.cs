using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;

namespace Sample.Client.GitHub
{
	public sealed class GitHubClient
	{
		private HttpClient client;
		public GitHubClient(HttpClient client)
		{
			this.client = client;
		}
		
		internal sealed class GraphQLRequest
        {
			public string query {get;set;}
			public Dictionary<string, object> variables {get;set;}
		}
		
        internal sealed class GraphQLResponse<T>
        {
            public IEnumerable<GraphQLResponseError> errors { get; set; }
            public T data { get; set; }

        }
        internal sealed class GraphQLResponseError
        {
            public string Message { get; set; }
        }

		public async Task<AddStarResult> AddStarAsync(string repositoyId)
		{
			var response = await this.client.PostAsync("", new StringContent(JsonConvert.SerializeObject(new GraphQLRequest()
            {
                query = @"mutation ($repositoyId : ID!){
  addStar(input :{clientMutationId : ""123"",  starrableId :$repositoyId }){
    starrable {
      viewerHasStarred
    }
  }
}
",
                variables = new Dictionary<string, object> {
					{ @"repositoyId", repositoyId },
                }
            })));

			if (!response.IsSuccessStatusCode)
            {
                throw new GitHubClientException(response);
            }

            var jsonResult = await response.Content.ReadAsStringAsync();
			
			var result = JsonConvert.DeserializeObject<GraphQLResponse<AddStarResult>>(jsonResult);
            if (result == null)
            {
                throw new GitHubClientException(response);
            }
            if (result.errors?.Any() == true) {
                throw new GitHubClientException(result.errors.Select(x=>x.Message), response);
            }
            
            return result.data;
		}

		public async Task<CurrentUserResult> CurrentUserAsync()
		{
			var response = await this.client.PostAsync("", new StringContent(JsonConvert.SerializeObject(new GraphQLRequest()
            {
                query = @"query  {
  viewer {
    login,
	bio,
  }
}",
                variables = new Dictionary<string, object> {
                }
            })));

			if (!response.IsSuccessStatusCode)
            {
                throw new GitHubClientException(response);
            }

            var jsonResult = await response.Content.ReadAsStringAsync();
			
			var result = JsonConvert.DeserializeObject<GraphQLResponse<CurrentUserResult>>(jsonResult);
            if (result == null)
            {
                throw new GitHubClientException(response);
            }
            if (result.errors?.Any() == true) {
                throw new GitHubClientException(result.errors.Select(x=>x.Message), response);
            }
            
            return result.data;
		}

		public async Task<SearchResult> SearchAsync(SearchType type, string query)
		{
			var response = await this.client.PostAsync("", new StringContent(JsonConvert.SerializeObject(new GraphQLRequest()
            {
                query = @"query ($type : SearchType!, $query : String!) {
  viewer {
    login
  }
  search(first : 10, type : $type, query : $query){
    nodes{
      __typename
			... on Issue {
        author {
          login
        }
      }
      ... on PullRequest{
        author {
          login
        }
      }
    }
  }
}",
                variables = new Dictionary<string, object> {
					{ @"type", type },
					{ @"query", query },
                }
            })));

			if (!response.IsSuccessStatusCode)
            {
                throw new GitHubClientException(response);
            }

            var jsonResult = await response.Content.ReadAsStringAsync();
			
			var result = JsonConvert.DeserializeObject<GraphQLResponse<SearchResult>>(jsonResult);
            if (result == null)
            {
                throw new GitHubClientException(response);
            }
            if (result.errors?.Any() == true) {
                throw new GitHubClientException(result.errors.Select(x=>x.Message), response);
            }
            
            return result.data;
		}

		public async Task<UsersRepositoresResult> UsersRepositoresAsync(string login, int? repoCount)
		{
			var response = await this.client.PostAsync("", new StringContent(JsonConvert.SerializeObject(new GraphQLRequest()
            {
                query = @"query ($login:String!, $repoCount: Int!){
  user(login: $login){
    login,
    bio,
    first :repositories(first : $repoCount){
      nodes{
        id,
        name,
        updatedAt
      }
    },
	last :repositories(last : $repoCount){
      nodes{
        id,
        name,
        updatedAt
      }
    }
  }
}",
                variables = new Dictionary<string, object> {
					{ @"login", login },
					{ @"repoCount", repoCount },
                }
            })));

			if (!response.IsSuccessStatusCode)
            {
                throw new GitHubClientException(response);
            }

            var jsonResult = await response.Content.ReadAsStringAsync();
			
			var result = JsonConvert.DeserializeObject<GraphQLResponse<UsersRepositoresResult>>(jsonResult);
            if (result == null)
            {
                throw new GitHubClientException(response);
            }
            if (result.errors?.Any() == true) {
                throw new GitHubClientException(result.errors.Select(x=>x.Message), response);
            }
            
            return result.data;
		}

	}
	
    public sealed class GitHubClientException : Exception
    {
        public HttpResponseMessage Response { get; private set; }
        public IEnumerable<string> ErrorMessages { get; private set; }

        public GitHubClientException(HttpResponseMessage response)
            : base("Error running graphql query, see response for more details")
        {
            this.Response = response;
        }

        public GitHubClientException(IEnumerable<string> errorMessages, HttpResponseMessage response)
            : base("Error running graphql query, error messages or response for more details")
        {
            this.Response = response;
            this.ErrorMessages = errorMessages;
        }
    }

	public sealed class StarrableResult
	{
		public bool? ViewerHasStarred { get; set; }
	}

	public sealed class AddStarPayloadResult
	{
		public StarrableResult Starrable { get; set; }
	}

	public sealed class AddStarResult
	{
		public AddStarPayloadResult AddStar { get; set; }
	}

	public sealed class UserResult
	{
		public string Login { get; set; }
		public string Bio { get; set; }
	}

	public sealed class CurrentUserResult
	{
		public UserResult Viewer { get; set; }
	}

	public sealed class User1Result
	{
		public string Login { get; set; }
	}

	public sealed class SearchResultItemResult
	{
		public string Typename { get; set; }
	}

	public sealed class SearchResultItemConnectionResult
	{
		public IEnumerable<SearchResultItemResult> Nodes { get; set; }
	}

	public sealed class SearchResult
	{
		public User1Result Viewer { get; set; }
		public SearchResultItemConnectionResult Search { get; set; }
	}

	public sealed class RepositoryResult
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public DateTime? UpdatedAt { get; set; }
	}

	public sealed class RepositoryConnectionResult
	{
		public IEnumerable<RepositoryResult> Nodes { get; set; }
	}

	public sealed class User2Result
	{
		public string Login { get; set; }
		public string Bio { get; set; }
		public RepositoryConnectionResult First { get; set; }
		public RepositoryConnectionResult Last { get; set; }
	}

	public sealed class UsersRepositoresResult
	{
		public User2Result User { get; set; }
	}


	public enum SearchType
	{
		ISSUE,
		REPOSITORY,
		USER,
	}

}