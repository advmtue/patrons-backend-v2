using System.Threading.Tasks;
using patrons_web_api.Models.MongoDatabase;
using patrons_web_api.Database;

namespace patrons_web_api.Services
{
    public interface IVenueService
    {
        Task<PublicVenueDocument> GetVenueById(string venueId);
        Task<PublicVenueDocument> GetVenueByURLName(string venueShort);
    }

    public class VenueService : IVenueService
    {
        private IPatronsDatabase _database;

        public VenueService(IPatronsDatabase database)
        {
            _database = database;
        }

        /// <summary>
        /// Gets public venue information for a given venue using unique ID
        /// </summary>
        /// <param name="venueId">Venue unique ID</param>
        /// <returns>A PublicVenue</returns>
        public async Task<PublicVenueDocument> GetVenueById(string venueId)
        {
            return await _database.GetVenueById(venueId);
        }

        /// <summary>
        /// Gets public venue information for a given venue using urlName
        /// </summary>
        /// <param name="urlName">Unique venue url name</param>
        /// <returns>A PublicVenue</returns>
        public async Task<PublicVenueDocument> GetVenueByURLName(string urlName)
        {
            return await _database.GetVenueByURLName(urlName);
        }
    }
}