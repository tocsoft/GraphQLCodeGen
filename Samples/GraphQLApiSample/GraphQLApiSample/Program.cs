using GraphQL.Conventions.Web;
using System;
using System.Diagnostics;

namespace GraphQLApiSample
{
    public class Program
    {
        public static void Main()
        {
            var handler = RequestHandler.New()
                .WithQuery(typeof(GqlQuery))
                .Generate();

            Console.WriteLine(handler.DescribeSchema(false, true, true));

            if (Debugger.IsAttached)
            {
                Console.ReadLine();
            }
        }
    }
}
