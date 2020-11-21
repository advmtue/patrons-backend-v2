using System.Collections.Generic;
using System.Threading.Tasks;
using patrons_web_api.Models.MongoDatabase;
using patrons_web_api.Database;

using patrons_web_api.Models.Transfer.Response;

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

        public async Task<VenueResponse> getVenueById(string venueId)
        {
            return await _database.getPatronVenueView(venueId);
        }
    }
}