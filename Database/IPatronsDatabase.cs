using System.Threading.Tasks;

using patrons_web_api.Models.MongoDatabase;
using patrons_web_api.Models.Transfer.Response;

namespace patrons_web_api.Database
{
    public interface IPatronsDatabase
    {
        Task<VenueDocument> getVenueInfo(string venueId);
        Task<VenueResponse> getPatronVenueView(string venueId);
    }
}