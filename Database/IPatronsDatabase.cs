using System.Threading.Tasks;

using patrons_web_api.Models.MongoDatabase;

namespace patrons_web_api.Database
{
    public interface IPatronsDatabase
    {
        Task<Venue> getVenueManagerInfo(string venueId);
        Task<VenueSimple> getVenueInfo(string venueId);
    }
}