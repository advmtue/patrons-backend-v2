using System.Linq;
using System.Threading.Tasks;
using patrons_web_api.Database;
using patrons_web_api.Models.Transfer.Request;

namespace patrons_web_api.Services
{
    public class PatronService
    {
        private IPatronsDatabase _database;

        public PatronService(IPatronsDatabase database)
        {
            // Save refs
            _database = database;
        }

        public async Task GamingCheckIn(string venueId, string areaId, GamingCheckInRequest checkIn)
        {
            // Perform gaming check-in
            await _database.GamingCheckIn(venueId, areaId, checkIn);
        }

        public async Task DiningCheckin(string venueId, string areaId, DiningCheckInRequest checkIn)
        {
            await _database.DiningCheckIn(venueId, areaId, checkIn);
        }
    }
}