using Microsoft.Graph;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace NegativeEddy.PresencePi.Graph
{
    public class CustomAuthenticationProvider : IAuthenticationProvider
    {
        public CustomAuthenticationProvider(Func<Task<string>> acquireTokenCallback)
        {
            acquireAccessToken = acquireTokenCallback;
        }

        private readonly Func<Task<string>> acquireAccessToken;

        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            string accessToken = await acquireAccessToken.Invoke();

            // Append the access token to the request.
            request.Headers.Authorization = new AuthenticationHeaderValue(
                Scopes.BearerAuthorizationScheme, accessToken);
        }
    }
}
