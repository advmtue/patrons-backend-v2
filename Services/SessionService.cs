using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

using Patrons.CheckIn.API.Database;

namespace Patrons.CheckIn.API.Services
{
    /// <summary>
    /// Session service configuration.
    /// </summary>
    public class SessionSettings : ISessionSettings
    {
        /// <summary>
        /// Required length of sessionID keys when generating new keys.
        /// </summary>
        public int KeyLength { get; set; }
    }

    public interface ISessionSettings
    {
        int KeyLength { get; set; }
    }

    public interface ISessionService
    {
        Task<string> GenerateSessionId();
    }

    public class SessionService : ISessionService
    {
        private IPatronsDatabase _db;
        public SessionService(IPatronsDatabase db)
        {
            // Save refs
            _db = db;
        }

        /// <summary>
        /// Attempt to generate a new session ID, checking the database that it doesn't already exist.
        /// </summary>
        /// <returns>Non-colliding sessionID string.</returns>
        public async Task<string> GenerateSessionId()
        {

            string sessionId;
            // Keep generating new sessionIDs until we find one that doesn't match.
            // The likelihood of a collision is extremely low, so this will likely only run once.
            do
            {
                // Allocate space for 128 bytes.
                byte[] data = new byte[128];

                // Use cryptographicalyl security random number generation to create some bytes.
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(data);
                }

                // Convert the bytes to a string, removing hypen characters.
                sessionId = BitConverter.ToString(data).Replace("-", "");
            } while (await _db.SessionExists(sessionId));

            // Return the non-colliding sessionId.
            return sessionId;
        }
    }
}
