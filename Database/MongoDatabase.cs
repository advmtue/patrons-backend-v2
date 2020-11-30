using System.Data;
using System;
using System.Collections.Generic;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Threading.Tasks;

using patrons_web_api.Models.MongoDatabase;

namespace patrons_web_api.Database
{
    // TODO Namespace exceptions somewhere else
    public class VenueNotFoundException : Exception { }
    public class VenueHasNoActiveServiceException : Exception { }
    public class VenueIsClosedException : Exception { }
    public class AreaNotFoundException : Exception { }
    public class AreaIsClosedException : Exception { }
    public class WrongAreaServiceTypeException : Exception { }
    public class SittingNotFoundException : Exception { }
    public class ServiceNotFoundException : Exception { }
    public class ManagerNotFoundException : Exception { }
    public class BadLoginException : Exception { }
    public class SessionExistsException : Exception { }
    public class SessionNotFoundException : Exception { }
    public class NoAccessException : Exception { }
    public class ServiceIsAlreadyActiveException : Exception { }

    public class ManagerVenueAggregation
    {
        public List<PublicVenueDocument> venues { get; set; }
    }

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

    public class PatronsCollectionNames
    {
        public const string Venue = "venue";
        public const string Service = "service";
        public const string Manager = "manager";
        public const string Session = "session";
    }

    public class MongoDatabase : IPatronsDatabase
    {
        private IMongoCollection<PublicVenueDocument> _venueCollection;
        private IMongoCollection<BsonDocument> _serviceCollection;
        private IMongoCollection<DiningServiceDocument> _diningServiceCollection;
        private IMongoCollection<GamingServiceDocument> _gamingServiceCollection;
        private IMongoCollection<ManagerDocument> _managerCollection;
        private IMongoCollection<SessionDocument> _sessionCollection;

        public MongoDatabase(IMongoDatabaseSettings settings)
        {
            // Create client connection
            var client = new MongoClient(
                $"mongodb://{settings.Username}:{settings.Password}@{settings.Host}/{settings.AuthDatabase}"
            );

            var database = client.GetDatabase(settings.UseDatabase);

            // Create collection refs
            _venueCollection = database.GetCollection<PublicVenueDocument>(PatronsCollectionNames.Venue);
            _serviceCollection = database.GetCollection<BsonDocument>(PatronsCollectionNames.Service);
            _managerCollection = database.GetCollection<ManagerDocument>(PatronsCollectionNames.Manager);
            _sessionCollection = database.GetCollection<SessionDocument>(PatronsCollectionNames.Session);

            // Dining and gaming services
            _diningServiceCollection = database.GetCollection<DiningServiceDocument>(PatronsCollectionNames.Service);
            _gamingServiceCollection = database.GetCollection<GamingServiceDocument>(PatronsCollectionNames.Service);
        }

        /// <summary>
        /// Lookup a venue by unique Id (venue._id)
        /// </summary>
        /// <param name="venueId">Venue ObjectId as string</param>
        /// <returns>Public venue information</returns>
        public async Task<PublicVenueDocument> GetVenueById(string venueId)
        {
            var filter = Builders<PublicVenueDocument>.Filter.Eq("_id", new ObjectId(venueId));

            var venue = await _venueCollection.Find(filter).FirstOrDefaultAsync();

            if (venue == null)
            {
                throw new VenueNotFoundException();
            }

            return venue;
        }

        /// <summary>
        /// Lookup a venue by unique url string (venue.venueId)
        /// </summary>
        /// <param name="urlName">Venue.venueId</param>
        /// <returns>Public venue information</returns>
        public async Task<PublicVenueDocument> GetVenueByURLName(string urlName)
        {
            var filter = Builders<PublicVenueDocument>.Filter.Eq("venueId", urlName);

            var venue = await _venueCollection.Find(filter).FirstOrDefaultAsync();

            if (venue == null)
            {
                throw new VenueNotFoundException();
            }

            return venue;
        }

        /// <summary>
        /// Save a gaming check-in by pushing a new patron to the patrons array
        /// </summary>
        /// <param name="serviceId">Service unique ID as string</param>
        /// <param name="patron">New patron</param>
        /// <returns></returns>
        public async Task SaveGamingCheckIn(string serviceId, GamingPatronDocument patron)
        {
            var filter = Builders<GamingServiceDocument>.Filter.Eq("_id", new ObjectId(serviceId));
            var update = Builders<GamingServiceDocument>.Update.Push("patrons", patron);

            await _gamingServiceCollection.UpdateOneAsync(filter, update);
        }

        /// <summary>
        /// Create or append a dining check-in, depending on if the table already exists in the service
        /// </summary>
        /// <param name="serviceId">Service unique ID as string</param>
        /// <param name="tableNumber">Table number</param>
        /// <param name="checkIn">New check-in</param>
        /// <returns></returns>
        public async Task CreateOrAppendDiningCheckIn(string serviceId, string tableNumber, CheckInDocument checkIn)
        {
            // Create a filter for matching service
            var serviceFilter = Builders<DiningServiceDocument>.Filter.Eq("_id", new ObjectId(serviceId))
                & Builders<DiningServiceDocument>.Filter.Eq("isActive", true);

            // Create a $push update
            var checkInsPushUpdate = Builders<DiningServiceDocument>.Update.Push("sittings.$[table].checkIns", checkIn);

            // Build array filters to match table with number and active status
            var arrayFilters = new List<ArrayFilterDefinition>
            {
                new BsonDocumentArrayFilterDefinition<DiningServiceDocument>(
                    new BsonDocument {
                        { "table.tableNumber", tableNumber },
                        { "table.isActive", true }
                    }
                )
            };
            var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters };

            // Attempt to perform the push
            var updateResult = await _diningServiceCollection.UpdateOneAsync(serviceFilter, checkInsPushUpdate, updateOptions);

            // Service with matching ID does not exist
            if (updateResult.MatchedCount == 0)
            {
                throw new ServiceNotFoundException();
            }

            // If a service was updated nothing else needs to be done
            if (updateResult.ModifiedCount == 1) return;

            // No service was update, so assume we need to create a new sitting
            var newSitting = new SittingDocument
            {
                Id = ObjectId.GenerateNewId().ToString(),
                TableNumber = tableNumber,
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                IsActive = true,
                CheckIns = new List<CheckInDocument> { checkIn }
            };

            // Create a $push update for sittings
            var sittingsPushUpdate = Builders<DiningServiceDocument>.Update.Push("sittings", newSitting);

            // Push the sitting to the service
            await _diningServiceCollection.UpdateOneAsync(serviceFilter, sittingsPushUpdate);
        }

        /// <summary>
        /// Find a manager from their username information
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>The manager with matching username</returns>
        /// <exception cref="patrons_web_api.Database.ManagerNotFoundException">
        /// Manager with matching username does not exist
        /// </exception>
        public async Task<ManagerDocument> GetManagerByUsername(string username)
        {
            var manager = await _managerCollection.Find(m => m.Username == username).FirstOrDefaultAsync();

            if (manager == null)
            {
                throw new ManagerNotFoundException();
            }

            return manager;
        }

        public async Task SaveSession(SessionDocument session)
        {
            // Ensure the session doesn't already exist
            // If the sessionID was generated using the correct means, this should never occur
            if (await this.SessionExists(session.SessionId))
            {
                throw new SessionExistsException();
            }

            _sessionCollection.InsertOne(session);
        }

        public async Task<bool> SessionExists(string sessionId)
        {
            var session = await _sessionCollection.Find(s => s.SessionId == sessionId).FirstOrDefaultAsync();

            return session != null;
        }

        public async Task<SessionDocument> GetSessionBySessionId(string sessionId)
        {
            var filter = Builders<SessionDocument>.Filter.Eq("sessionId", sessionId);

            var session = await _sessionCollection.Find(filter).FirstOrDefaultAsync();

            if (session == null)
            {
                throw new SessionNotFoundException();
            }

            return session;
        }

        public async Task<ManagerDocument> GetManagerById(string managerId)
        {
            var manager = await _managerCollection.Find(m => m.Id.Equals(new ObjectId(managerId))).FirstOrDefaultAsync();

            if (manager == null)
            {
                throw new ManagerNotFoundException();
            }

            return manager;
        }

        public async Task ManagerUpdatePassword(string managerId, string newPassword)
        {
            // Filter to match manager._id with ObjectId(managerId)
            var filter = Builders<ManagerDocument>.Filter.Eq("_id", new ObjectId(managerId));

            // Update to set password to the new value and isPasswordReset = false
            // TODO Password field name as a registered constant
            var update = Builders<ManagerDocument>.Update.Set("password", newPassword).Set("isPasswordReset", false);

            // Update manager document
            await _managerCollection.UpdateOneAsync(filter, update);
        }

        public async Task ManagerDeactivateSessions(string managerId)
        {
            // Filter to match session.managerId with ObjectId(managerId)
            var filter = Builders<SessionDocument>.Filter.Eq("managerId", new ObjectId(managerId));

            // Update to disable the session
            var update = Builders<SessionDocument>.Update.Set("isActive", false);

            await _sessionCollection.UpdateManyAsync(filter, update);
        }

        private async Task<DiningServiceDocument> GetDiningServiceById(string serviceId)
        {
            var filter = Builders<DiningServiceDocument>.Filter.Eq("_id", new ObjectId(serviceId));
            var service = await _diningServiceCollection.Find(filter).FirstOrDefaultAsync();

            if (service == null)
            {
                throw new ServiceNotFoundException();
            }

            return service;
        }

        private async Task<GamingServiceDocument> GetGamingServiceById(string serviceId)
        {
            var filter = Builders<GamingServiceDocument>.Filter.Eq("_id", new ObjectId(serviceId));
            var service = await _gamingServiceCollection.Find(filter).FirstOrDefaultAsync();

            if (service == null)
            {
                throw new ServiceNotFoundException();
            }

            return service;
        }

        public async Task<List<ManagerVenueDocument>> GetManagerVenues(string managerId)
        {
            var result = _managerCollection.Aggregate()
                .Match(m => m.Id.Equals(new ObjectId(managerId)))
                .Lookup("venue", "venueIds", "_id", "venues")
                .Project<ManagerVenueAggregation>(new BsonDocument { { "_id", 0 }, { "venues", 1 } });

            var firstResult = result.First().venues;

            // TODO this is very ineffecient and can be drastically improved with an aggregation pipeline
            // TODO Lookup gaming/dining area activeService
            // Perform further lookups for active service
            var managerVenues = new List<ManagerVenueDocument>();

            firstResult.ForEach(v =>
            {
                managerVenues.Add(new ManagerVenueDocument(v));
            });

            // Populate each managervenuedocument.areas.activeService by performing a round trip
            // This is awful
            // FIXME
            foreach (var venue in managerVenues)
            {
                foreach (var da in venue.DiningAreas)
                {
                    if (da.ActiveService.Id == null) continue;
                    da.ActiveService = await this.GetDiningServiceById(da.ActiveService.Id.ToString());
                }

                foreach (var ga in venue.GamingAreas)
                {
                    if (ga.ActiveService.Id == null) continue;
                    ga.ActiveService = await this.GetGamingServiceById(ga.ActiveService.Id.ToString());
                }
            }

            return managerVenues;
        }

        public async Task<bool> ManagerCanAccessVenue(string managerId, string venueId)
        {
            var manager = await this.GetManagerById(managerId);

            bool canAccess = false;
            manager.VenueIds.ForEach(v =>
            {
                if (v.Equals(new ObjectId(venueId)))
                {
                    canAccess = true;
                }
            });

            return canAccess;
        }
    }
}