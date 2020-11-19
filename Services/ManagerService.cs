using System.Collections.Concurrent;
using System;
using System.Threading.Tasks;

using patrons_web_api.Models.MongoDatabase;

using patrons_web_api.Database;

namespace patrons_web_api.Services
{
    public class ManagerService
    {
        private IPatronsDatabase _database;

        public ManagerService(IPatronsDatabase db)
        {
            Console.WriteLine("Instantiated new managerservice");

            // Save refs
            _database = db;
        }

        public async Task<Venue> getPublicVenueInfo(string venueId)
        {
            return await _database.getVenueManagerInfo(venueId);
        }
    }
}