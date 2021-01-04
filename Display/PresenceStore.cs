using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using System;
using System.Security.Claims;
using Microsoft.Graph;


namespace NegativeEddy.PresencePi.Display
{
    public class PresenceStore
    {
        private readonly ILogger<PresenceStore> _logger;

        public PresenceStore(ILogger<PresenceStore> logger)
        {
            _logger = logger;
        }

        public readonly Presence PresenceError = new Presence() { Activity = "ERROR", Availability = "ERROR" };

        private static Presence _presence = new Presence() { Activity = "Unknown", Availability = "Unknown" };

        public Presence Presence
        {
            get => _presence;
            set
            {
                if (value.Activity != _presence.Activity || value.Availability != _presence.Availability)
                {
                    PresenceChanging?.Invoke(this, new PresenceChangingEventArgs { Current = _presence, New = value });
                    _presence = value;
                    _logger.LogTrace("presence changed to {Availability}:{Activity}", _presence.Availability, _presence.Activity);
                }
            }
        }

        public event EventHandler<PresenceChangingEventArgs> PresenceChanging;

        public ITokenAcquisition TokenAcquisition { get; internal set; }
        public ClaimsPrincipal User { get; internal set; }
    }
}
