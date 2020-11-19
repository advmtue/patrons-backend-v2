using System;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Threading.Tasks;

using patrons_web_api.Models.MongoDatabase;

namespace patrons_web_api.Database
{
    public class VenueNotFoundException : Exception { }

    public class MongoDatabaseSettings : IMongoDatabaseSettings
    {
        public string Host { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string AuthDatabase { get; set; }
        public string UseDatabase { get; set; }
    }

    public interface IMongoDatabaseSettings
    {
        string Host { get; set; }
        string Username { get; set; }
        string Password { get; set; }
        string AuthDatabase { get; set; }
        string UseDatabase { get; set; }
    }

    public class MongoDatabase : IPatronsDatabase
    {
        private IMongoCollection<Venue> _venueCollection;

        public MongoDatabase(IMongoDatabaseSettings settings)
        {
            // Create client connection
            var client = new MongoClient(
                $"mongodb://{settings.Username}:{settings.Password}@{settings.Host}/{settings.AuthDatabase}"
            );

            var database = client.GetDatabase(settings.UseDatabase);

            _venueCollection = database.GetCollection<Venue>("venue");
        }

        public async Task<Venue> getVenueManagerInfo(string venueId)
        {
            var venue = await _venueCollection.Find<Venue>(v => v.VenueId == venueId).FirstOrDefaultAsync();

            if (venue == null)
            {
                throw new VenueNotFoundException();
            }

            return venue;
        }

        public async Task<VenueSimple> getVenueInfo(string venueId)
        {
            var venue = await this.getVenueManagerInfo(venueId);

            return venue.toVenueSimple();
        }
    }
}