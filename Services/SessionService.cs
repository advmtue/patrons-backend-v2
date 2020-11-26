using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

using patrons_web_api.Database;

namespace patrons_web_api.Services
{
    public class SessionSettings : ISessionSettings
    {
        public int KeyLength { get; set; }
    }

    public interface ISessionSettings
    {
        int KeyLength { get; set; }
    }

    public class SessionService
    {
        private IPatronsDatabase _db;
        public SessionService(IPatronsDatabase db)
        {
            // Save refs
            _db = db;
        }

        public async Task<string> GenerateSessionId()
        {

            string sessionId;
            do
            {
                // Generate a new session ID from crypto
                byte[] data = new byte[128];

                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(data);
                }

                sessionId = BitConverter.ToString(data).Replace("-", "");
            } while (await _db.SessionExists(sessionId));

            return sessionId;
        }
    }
}