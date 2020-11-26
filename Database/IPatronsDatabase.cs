using System.Threading.Tasks;

using patrons_web_api.Models.MongoDatabase;
using patrons_web_api.Models.Transfer.Response;
using patrons_web_api.Models.Transfer.Request;

namespace patrons_web_api.Database
{
    public interface IPatronsDatabase
    {
        Task<VenueDocument> getVenueInfo(string venueId);
        Task<VenueResponse> getPatronVenueView(string venueId, string indexName = "venueId");

        Task GamingCheckIn(string venueId, string areaId, GamingCheckInRequest checkIn);
        Task DiningCheckIn(string venueId, string areaId, DiningCheckInRequest checkIn);

        Task<SittingDocument> FindActiveSittingByServiceAndTable(string serviceId, string tableNumber);

        Task<ManagerDocument> GetManagerByUsername(string username);
        Task<ManagerDocument> GetManagerById(string managerId);
        Task ManagerUpdatePassword(string managerId, string newPasswordHash);
        Task ManagerDeactivateSessions(string managerId);

        Task SaveSession(SessionDocument session);
        Task<bool> SessionExists(string sessionId);
        Task<SessionDocument> GetSessionBySessionId(string sessionId);
    }
}