using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using patrons_web_api.Database;

/// <summary>
/// Authentication handler for Session ID style authentication mechanism.
/// </summary>
namespace patrons_web_api.Authentication
{
    public class SessionIdAuthenticationSchemeOptions : AuthenticationSchemeOptions { }

    public class SessionIdAuthenticationHandler : AuthenticationHandler<SessionIdAuthenticationSchemeOptions>
    {
        private IPatronsDatabase _db;

        public SessionIdAuthenticationHandler(
            IOptionsMonitor<SessionIdAuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IPatronsDatabase db
        ) : base(options, logger, encoder, clock)
        {
            // Save database refs
            _db = db;
        }

        /// <summary>
        /// Attempt to authenticate a HTTP request by parsing the authorization header.
        /// </summary>
        /// <returns>Result of authentication.</returns>
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Ensure that the request contains authorization headers.
            if (!Request.Headers.ContainsKey("Authorization"))
            {
                return AuthenticateResult.NoResult();
            }

            // Save the sessionID to a local sessionId variable.
            string sessionId = Request.Headers["Authorization"].ToString();

            SessionDocument session;
            try
            {
                // Lookup the session by ID in the database.
                session = await _db.GetSessionBySessionId(sessionId);
            }
            catch (SessionNotFoundException)
            {
                // Error: Session does not exist.
                // Fail authentication.
                return AuthenticateResult.Fail("Specified session does not exist");
            }

            // If the session has expired...
            if (!session.IsActive)
            {
                // Fail authentication
                return AuthenticateResult.Fail("Session has expired");
            }

            // Construct a new claims identity using the manager's ID and access level.
            var claims = new[] {
                new Claim(ClaimTypes.Name, session.ManagerId),
                new Claim(ClaimTypes.Role, session.AccessLevel),
            };

            // Establish a claims identity for the sessionIdAuthenticationHandler.
            var claimsIdentity = new ClaimsIdentity(claims, nameof(SessionIdAuthenticationHandler));

            // Create an authentication ticket from the established claimsIdentity.
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(claimsIdentity), this.Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
    }
}