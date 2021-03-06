using System.Threading.Tasks;
using System.Collections.Generic;

using Patrons.CheckIn.API.Controllers;
using Patrons.CheckIn.API.Models.MongoDatabase;

namespace Patrons.CheckIn.API.Database
{
    // TODO Split the database either into partials of the class or more modular component architecture
    // At the moment this is a monolith and I do not like it.
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
        Task ManagerUpdatePassword(string managerId, string newPasswordHash, string newSalt);
        Task ManagerDeactivateSessions(string managerId);
        Task<List<ManagerVenueDocument>> GetManagerVenues(string managerId);
        Task<bool> ManagerCanAccessVenue(string managerId, string venueId);
        Task<bool> ManagerCanAccessService(string managerId, string serviceId);


        // Manager -- Service Actions
        Task<DiningServiceDocument> StartDiningService(string venueId, string areaId);
        Task<GamingServiceDocument> StartGamingService(string venueId, string areaId);
        Task StopDiningService(string venueId, string areaId);
        Task StopGamingService(string venueID, string areaId);


        // Manager -- Dining Actions
        Task DeleteDiningPatron(string serviceId, string tableId, string checkInId, string patronId);
        Task UpdateDiningPatron(string serviceId, string tableId, string checkInId, string patronId, DiningPatronDocument patron);
        Task<string> MoveDiningGroup(string serviceId, string tableId, string checkInId, string newTableNumber);
        Task<string> MoveDiningTable(string serviceId, string tableId, string newTableNumber);
        Task CloseDiningTable(string serviceId, string tableId);


        // Manager -- Gaming Actions
        Task DeleteGamingPatron(string serviceId, string patronId);
        Task UpdateGamingPatron(string serviceId, string patronId, GamingPatronUpdateRequest patron);
        Task CheckOutGamingPatron(string serviceId, string patronId);

        // Manager -- Get dining and gaming services
        Task<DiningServiceDocument> GetDiningServiceById(string serviceId);
        Task<GamingServiceDocument> GetGamingServiceById(string serviceId);

        // Session
        Task SaveSession(SessionDocument session);
        Task<bool> SessionExists(string sessionId);
        Task<SessionDocument> GetSessionBySessionId(string sessionId);

        // Marketing users
        Task<MarketingUser> GetActiveMarketingUserByEmail(string email);
        Task SetMarketingUserSubscription(string id, bool isSubscribed);
        Task<MarketingUser> CreateMarketingUser(string name, string email);
        Task<string> CreateMarketingUserUnsubscribeLink(MarketingUser mUser);
        Task UnsubscribeFromMarketing(string unsubscribeId);
    }
}
