using System.Threading.Tasks;
using System.Collections.Generic;


using patrons_web_api.Models.MongoDatabase;

namespace patrons_web_api.Database
{
    public interface IPatronsDatabase
    {
        // Public venue info
        Task<PublicVenueDocument> GetVenueById(string venueId);
        Task<PublicVenueDocument> GetVenueByURLName(string urlName);


        // Patron
        Task SaveGamingCheckIn(string serviceId, GamingPatronDocument patron);
        Task CreateOrAppendDiningCheckIn(string serviceId, string tableNumber, CheckInDocument checkIn);


        // Manager
        Task<ManagerDocument> GetManagerByUsername(string username);
        Task<ManagerDocument> GetManagerById(string managerId);
        Task ManagerUpdatePassword(string managerId, string newPasswordHash);
        Task ManagerDeactivateSessions(string managerId);
        Task<List<ManagerVenueDocument>> GetManagerVenues(string managerId);

        Task<bool> ManagerCanAccessVenue(string managerId, string venueId);

        // Session
        Task SaveSession(SessionDocument session);
        Task<bool> SessionExists(string sessionId);
        Task<SessionDocument> GetSessionBySessionId(string sessionId);
    }
}