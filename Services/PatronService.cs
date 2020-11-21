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
    }
}