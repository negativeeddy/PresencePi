using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Graph = Microsoft.Graph;
using Microsoft.Identity.Web;
using NegativeEddy.PresencePi.Models;
using NegativeEddy.PresencePi.Graph;
using Microsoft.Extensions.Logging;
using NegativeEddy.PresencePi.Display;

namespace NegativeEddy.PresencePi.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly ILogger<HomeController> _logger;
        private readonly PresenceStore _presenceStore;
        private readonly WebOptions _webOptions;

        public HomeController(ITokenAcquisition tokenAcquisition,
                              IOptions<WebOptions> webOptionValue,
                              ILogger<HomeController> logger,
                              PresenceStore presenceService)
        {
            _tokenAcquisition = tokenAcquisition;
            _logger = logger;
            _presenceStore = presenceService;
            _webOptions = webOptionValue.Value;
        }

        [AuthorizeForScopes(Scopes = new[] { Scopes.ScopeUserRead, Scopes.PresenceRead })]
        public async Task<IActionResult> Index()
        {
            // Initialize the GraphServiceClient. 
            Graph::GraphServiceClient graphClient = GetGraphServiceClient(new[] { Scopes.ScopeUserRead, Scopes.PresenceRead });

            var me = await graphClient.Me.Request().GetAsync();

            try
            {
                var presence = await graphClient.Me.Presence.Request().GetAsync();
                ViewData["Presence"] = presence;
            }
            catch (Exception ex)
            {
                ViewData["Presence"] = null;
                _logger.LogError(ex, "failed to get presence");
            }

            try
            {
                // Get user photo
                using (var photoStream = await graphClient.Me.Photo.Content.Request().GetAsync())
                {
                    byte[] photoByte = ((MemoryStream)photoStream).ToArray();
                    ViewData["Photo"] = Convert.ToBase64String(photoByte);
                }
            }
            catch (System.Exception)
            {
                ViewData["Photo"] = null;
            }

            return View();
        }

        private Graph::GraphServiceClient GetGraphServiceClient(string[] scopes)
        {
            return GraphServiceClientFactory.GetAuthenticatedGraphClient(async () =>
            {

                string result = await _tokenAcquisition.GetAccessTokenForUserAsync(scopes);

                // save the services for the PresenceUpdaterService's use
                _presenceStore.TokenAcquisition = _tokenAcquisition;
                _presenceStore.User = HttpContext.User;

                return result;
            }, _webOptions.GraphApiUrl);
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}