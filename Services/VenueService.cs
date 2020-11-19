using System.Threading.Tasks;
using patrons_web_api.Models.MongoDatabase;
using patrons_web_api.Database;

namespace patrons_web_api.Services
{
    public class VenueService
    {
        private IPatronsDatabase _database;

        public VenueService(IPatronsDatabase database)
        {
            // Save refs
            _database = database;
        }

        public Task<VenueSimple> getVenueById(string venueId)
        {
            return _database.getVenueInfo(venueId);
        }

        public Task<Venue> getVenueManagerInfo(string venueId)
        {
            return _database.getVenueManagerInfo(venueId);
        }
    }
}