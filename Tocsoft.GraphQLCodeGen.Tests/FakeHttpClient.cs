using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tocsoft.GraphQLCodeGen.Tests
{
    public class GraphQlQuery
    {
        public string Query { get; set; } = "";
        public Dictionary<string, JToken> Variables { get; set; } = new Dictionary<string, JToken>();
    }

    public static class FakeHttpClientGraphQlExtensions
    {
        public static void SetupGraphqlRequest<TResult>(this FakeHttpClient @this, Func<GraphQlQuery, TResult> intercepter)
            => @this.Intercept(HttpMethod.Post, null, (request) =>
            {
                var json = request.Content.ReadAsStringAsync().Result;
                var query = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQlQuery>(json);
                var result = intercepter(query);
                var responseJson = Newtonsoft.Json.JsonConvert.SerializeObject(result);
                var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
                response.Content = new StringContent(responseJson, Encoding.UTF8, "application/json");
                return response;
            });
    }

    public class FakeHttpClient : System.Net.Http.HttpClient
    {
        private readonly FakeHttpMessageHandler handler;

        public static FakeHttpClient Create()
        {
            var handler = new FakeHttpMessageHandler();
            return new FakeHttpClient(handler);
        }

        private FakeHttpClient(FakeHttpMessageHandler handler)
            : base(handler)
        {
            this.handler = handler;
            this.BaseAddress = new Uri("http://localhost.test/endpoint");
        }

        public void Post(System.Net.Http.HttpMethod method, Uri uri, Func<System.Net.Http.HttpRequestMessage, System.Net.Http.HttpResponseMessage> intercepter)
            => this.Intercept(HttpMethod.Post, uri, intercepter);

        public void Intercept(System.Net.Http.HttpMethod method, Uri uri, Func<System.Net.Http.HttpRequestMessage, System.Net.Http.HttpResponseMessage> intercepter)
        {
            if (uri == null)
            {
                uri = this.BaseAddress;
            }

            if (!uri.IsAbsoluteUri)
            {
                uri = new Uri(this.BaseAddress, uri);
            }

            this.handler.Intercept(method, uri, intercepter);
        }
        internal void Verify(System.Net.Http.HttpMethod method, Uri uri)
            => this.handler.Verify(method, uri);

        internal void VerifyAll()
            => this.handler.VerifyAll();

        private class FakeHttpMessageHandler : System.Net.Http.HttpMessageHandler
        {
            private Dictionary<(System.Net.Http.HttpMethod method, Uri uri), Func<System.Net.Http.HttpRequestMessage, System.Net.Http.HttpResponseMessage>> intercepters
             = new Dictionary<(System.Net.Http.HttpMethod method, Uri uri), Func<System.Net.Http.HttpRequestMessage, System.Net.Http.HttpResponseMessage>>();

            private List<(System.Net.Http.HttpMethod method, Uri uri)> calledIntercepters = new List<(HttpMethod method, Uri uri)>();

            internal FakeHttpMessageHandler()
            {
            }

            internal void Intercept(System.Net.Http.HttpMethod method, Uri uri, Func<System.Net.Http.HttpRequestMessage, System.Net.Http.HttpResponseMessage> intercepter)
            {
                this.intercepters[(method, uri)] = intercepter;
            }

            internal void Verify(System.Net.Http.HttpMethod method, Uri uri)
            {
                Assert.Contains((method, uri), calledIntercepters);
            }
            internal void VerifyAll()
            {
                Assert.All(intercepters.Keys, a =>
                {
                    Verify(a.method, a.uri);
                });
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (this.intercepters.TryGetValue((request.Method, request.RequestUri), out var func))
                {
                    calledIntercepters.Add((request.Method, request.RequestUri));
                    var resp = func(request);
                    return Task.FromResult(resp);
                }

                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotImplemented));
            }
        }
    }
}
