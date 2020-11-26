using System.Security.Claims;
using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using patrons_web_api.Database;

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

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization"))
            {
                return AuthenticateResult.Fail("Authorization header not present in request");
            }

            string sessionId = Request.Headers["Authorization"].ToString();

            // Lookup session
            SessionDocument session;
            try
            {
                session = await _db.GetSessionBySessionId(sessionId);
            }
            catch (SessionNotFoundException)
            {
                return AuthenticateResult.Fail("Specified session does not exist");
            }

            // Session has expired
            if (!session.IsActive)
            {
                return AuthenticateResult.Fail("Session has expired");
            }

            // Construct claims identity
            var claims = new[] {
                new Claim(ClaimTypes.Name, session.ManagerId),
                new Claim(ClaimTypes.Role, session.AccessLevel),
            };

            var claimsIdentity = new ClaimsIdentity(claims, nameof(SessionIdAuthenticationHandler));
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(claimsIdentity), this.Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
    }

}