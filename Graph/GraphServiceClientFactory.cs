using Microsoft.Graph;
using System;
using System.Threading.Tasks;

namespace NegativeEddy.PresencePi.Graph
{
    public class GraphServiceClientFactory
    {
        public static GraphServiceClient GetAuthenticatedGraphClient(Func<Task<string>> acquireAccessToken, 
                                                                                 string baseUrl)
        {
  
            return new GraphServiceClient(baseUrl, new CustomAuthenticationProvider(acquireAccessToken));
        }
    }
}
