using System.Threading;
using System;
using System.Collections.Generic;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Threading.Tasks;

using patrons_web_api.Models.MongoDatabase;
using patrons_web_api.Controllers;

namespace patrons_web_api.Database
{
    // TODO Namespace exceptions somewhere else
    public class VenueNotFoundException : Exception { }
    public class AreaHasNoActiveServiceException : Exception { }
    public class AreaHasActiveServiceException : Exception { }
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
    public class ServiceIsNotActiveException : Exception { }
    public class TableNotFoundException : Exception { }
    public class CheckInNotFoundExcption : Exception { }
    public class PatronNotFoundException : Exception { }
    public class MatchingTableException : Exception { }
    public class MarketingUserNotFoundException : Exception { }
    public class MarketingUnsubscribeNotFound : Exception { }
    public class MarketingUnsubscribeAlreadyUsed : Exception { }

    /// <summary>
    /// Output of aggregation pipeline for manager venue.
    /// </summary>
    public class ManagerVenueAggregation
    {
        public List<PublicVenueDocument> venues { get; set; }
    }

    /// <summary>
    /// Mongo database connection settings
    /// </summary>
    public class MongoDatabaseSettings : IMongoDatabaseSettings
    {
        /// <summary>
        /// Hostname.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Auth username.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Auth password.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Authentication Database.
        /// </summary>
        public string AuthDatabase { get; set; }

        /// <summary>
        /// Business database.
        /// </summary>
        /// <value></value>
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

    /// <summary>
    /// Const collection names.
    /// </summary>
    public class PatronsCollectionNames
    {
        public const string Venue = "venue";
        public const string Service = "service";
        public const string Manager = "manager";
        public const string Session = "session";
        public const string MarketingUser = "marketing_user";
        public const string MarketingUnsubscribe = "marketing_user_unsubscribe";
    }

    public class MongoDatabase : IPatronsDatabase
    {
        private IMongoClient _client;
        private IMongoCollection<PublicVenueDocument> _venueCollection;
        private IMongoCollection<BsonDocument> _serviceCollection;
        private IMongoCollection<DiningServiceDocument> _diningServiceCollection;
        private IMongoCollection<GamingServiceDocument> _gamingServiceCollection;
        private IMongoCollection<ManagerDocument> _managerCollection;
        private IMongoCollection<SessionDocument> _sessionCollection;
        private IMongoCollection<MarketingUser> _marketingUserCollection;
        private IMongoCollection<MarketingUnsubscribe> _marketingUnsubscribeCollection;

        public MongoDatabase(IMongoDatabaseSettings settings)
        {
            // Create a new mongo database connection.
            _client = new MongoClient(
                $"mongodb://{settings.Username}:{settings.Password}@{settings.Host}/{settings.AuthDatabase}"
            );

            // Use the patron's database.
            var database = _client.GetDatabase(settings.UseDatabase);

            // Collection references for venue, service, manager, session.
            _venueCollection = database.GetCollection<PublicVenueDocument>(PatronsCollectionNames.Venue);
            _serviceCollection = database.GetCollection<BsonDocument>(PatronsCollectionNames.Service);
            _managerCollection = database.GetCollection<ManagerDocument>(PatronsCollectionNames.Manager);
            _sessionCollection = database.GetCollection<SessionDocument>(PatronsCollectionNames.Session);

            // Collection references for marketing users and unsubscriptions
            _marketingUserCollection = database.GetCollection<MarketingUser>(PatronsCollectionNames.MarketingUser);
            _marketingUnsubscribeCollection = database.GetCollection<MarketingUnsubscribe>(PatronsCollectionNames.MarketingUnsubscribe);

            // Dining and gaming services
            _diningServiceCollection = database.GetCollection<DiningServiceDocument>(PatronsCollectionNames.Service);
            _gamingServiceCollection = database.GetCollection<GamingServiceDocument>(PatronsCollectionNames.Service);
        }

        /// <summary>
        /// Lookup a venue by unique Id (venue._id)
        /// </summary>
        /// <param name="venueId">Venue Bson ID</param>
        /// <returns>Public venue information</returns>
        public async Task<PublicVenueDocument> GetVenueById(string venueId)
        {
            // Create a filter to match venue.id to venueId.
            var filter = Builders<PublicVenueDocument>.Filter.Eq(v => v.Id, venueId);

            // Search for a matching venue asynchronously.
            var venue = await _venueCollection.Find(filter).FirstOrDefaultAsync();

            // Throw an exception if no matching venue was found.
            if (venue == null) throw new VenueNotFoundException();

            // Return the public venue information.
            return venue;
        }

        /// <summary>
        /// Lookup a venue by unique url string (venue.venueId)
        /// </summary>
        /// <param name="urlName">Venue URL name</param>
        /// <returns>Public venue information</returns>
        public async Task<PublicVenueDocument> GetVenueByURLName(string urlName)
        {
            // Create a filter to match venue.venueId to urlName.
            var filter = Builders<PublicVenueDocument>.Filter.Eq(v => v.VenueId, urlName);

            // Search for a matching venue.
            var venue = await _venueCollection.Find(filter).FirstOrDefaultAsync();

            // Throw an exception if no matching venue was found.
            if (venue == null) throw new VenueNotFoundException();

            // Return the public venue information.
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
            // Build a filter to match the gaming service ID to serviceID.
            var filter = Builders<GamingServiceDocument>.Filter.Eq(gs => gs.Id, serviceId);

            // Build an upate to push a new patron to the service.
            var update = Builders<GamingServiceDocument>.Update.Push(gs => gs.Patrons, patron);

            // Attempt to perform the update.
            await _gamingServiceCollection.UpdateOneAsync(filter, update);
        }

        /// <summary>
        /// Create or append a dining check-in, depending on if the table already exists in the service
        /// </summary>
        /// <param name="serviceId">Service unique ID as string</param>
        /// <param name="tableNumber">Table number</param>
        /// <param name="checkIn">New check-in</param>
        public async Task CreateOrAppendDiningCheckIn(string serviceId, string tableNumber, CheckInDocument checkIn)
        {
            // Build a filter to match the diningService.id to serviceId, and ensure it is the active service.
            var serviceFilter = Builders<DiningServiceDocument>.Filter.Eq(ds => ds.Id, serviceId)
                & Builders<DiningServiceDocument>.Filter.Eq(ds => ds.IsActive, true);

            // Build an update to push the new checkIn to the table.
            var checkInsPushUpdate = Builders<DiningServiceDocument>.Update.Push("sittings.$[table].checkIns", checkIn);

            // Build array filters to match table with number and active status.
            var arrayFilters = new List<ArrayFilterDefinition>
            {
                new BsonDocumentArrayFilterDefinition<DiningServiceDocument>(
                    new BsonDocument {
                        { "table.tableNumber", tableNumber },
                        { "table.isActive", true }
                    }
                )
            };

            // Create update options using the array filters.
            var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters };

            // Attempt to push the checkIns to the dining service.
            var updateResult = await _diningServiceCollection.UpdateOneAsync(serviceFilter, checkInsPushUpdate, updateOptions);

            // Throw an error if update failed since there was no matching service.
            if (updateResult.MatchedCount == 0) throw new ServiceNotFoundException();

            // If a service was updated nothing else needs to be done.
            if (updateResult.ModifiedCount == 1) return;

            // No service was updated, so assume we need to create a new sitting.
            var newSitting = new SittingDocument
            {
                Id = ObjectId.GenerateNewId().ToString(),
                TableNumber = tableNumber,
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                IsActive = true,
                // Check-ins is this singular checkIn since we are creating a new sitting
                CheckIns = new List<CheckInDocument> { checkIn }
            };

            // Create a push update for the sitting into the dining services.
            var sittingsPushUpdate = Builders<DiningServiceDocument>.Update.Push(ds => ds.Sittings, newSitting);

            // Perform the push of the sitting into the service.
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
            // Lookup manager information.
            var manager = await _managerCollection.Find(m => m.Username == username).FirstOrDefaultAsync();

            // Throw an exception if the manager does not exist.
            if (manager == null) throw new ManagerNotFoundException();

            // Return the manager information.
            return manager;
        }

        /// <summary>
        /// Save a new session to the sessions collection.
        /// </summary>
        /// <param name="session">Session document information</param>
        public async Task SaveSession(SessionDocument session)
        {
            // Throw an exception if the session already exists.
            if (await this.SessionExists(session.SessionId))
            {
                throw new SessionExistsException();
            }

            // Save the session.
            _sessionCollection.InsertOne(session);
        }

        /// <summary>
        /// Check if a session already exists with teh same sessionId.
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <returns>True if a matching session exists.</returns>
        public async Task<bool> SessionExists(string sessionId)
        {
            // Lookup the session.
            // Intentionally don't use this.GetSessionBySessionId since it throws a SessionNountFoundException.
            var session = await _sessionCollection.Find(s => s.SessionId == sessionId).FirstOrDefaultAsync();

            return session != null;
        }

        /// <summary>
        /// Lookup a session by its session ID
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public async Task<SessionDocument> GetSessionBySessionId(string sessionId)
        {
            var session = await _sessionCollection
                .Find(s => s.SessionId == sessionId)
                .FirstOrDefaultAsync();

            // Throw an exception if the session does not exist.
            if (session == null) throw new SessionNotFoundException();

            // Return the session.
            return session;
        }

        /// <summary>
        /// Lookup a manager by their manager ID.
        /// </summary>
        /// <param name="managerId">Manager ID</param>
        /// <returns>Matching manager.</returns>
        public async Task<ManagerDocument> GetManagerById(string managerId)
        {
            // Find a manager whose ID matches supplied managerId.
            var manager = await _managerCollection
                .Find(m => m.Id == managerId)
                .FirstAsync();

            // Throw an exception if no manager is found.
            if (manager == null) throw new ManagerNotFoundException();

            // Return the manager.
            return manager;
        }

        /// <summary>
        /// Update a manager's password to a new password and salt combination.
        /// </summary>
        /// <param name="managerId">Target manager's ID.</param>
        /// <param name="newPassword">New password string</param>
        /// <param name="newSalt">Salt string</param>
        public async Task ManagerUpdatePassword(string managerId, string newPassword, string newSalt)
        {
            // Build a filter to match the manager ID with supplied managerId.
            var filter = Builders<ManagerDocument>.Filter.Eq(m => m.Id, managerId);

            // Build an update to assign new password, salt, and isPasswordReset = false.
            var update = Builders<ManagerDocument>.Update
                .Set(m => m.Password, newPassword)
                .Set(m => m.Salt, newSalt)
                .Set(m => m.IsPasswordReset, false);

            // Update manager document.
            await _managerCollection.UpdateOneAsync(filter, update);
        }

        /// <summary>
        /// Deactivate all sessions for a manager specified by ID.
        /// </summary>
        /// <param name="managerId">Target manager ID</param>
        public async Task ManagerDeactivateSessions(string managerId)
        {
            // Build a filter to match session.managerId with supplied managerId.
            var filter = Builders<SessionDocument>.Filter.Eq(s => s.ManagerId, managerId);

            // Build an update to disable the session.
            var update = Builders<SessionDocument>.Update.Set(s => s.IsActive, false);

            // Update all matching documents.
            await _sessionCollection.UpdateManyAsync(filter, update);
        }

        /// <summary>
        /// Lookup a dining service by ID.
        /// </summary>
        /// <param name="serviceId">Target service ID</param>
        /// <returns>The dining service.</returns>
        public async Task<DiningServiceDocument> GetDiningServiceById(string serviceId)
        {
            // Find the first service which matches the filter.
            var service = await _diningServiceCollection
                .Find(ds => ds.Id == serviceId)
                .FirstOrDefaultAsync();

            // Throw an exception if no matching service is found.
            if (service == null) throw new ServiceNotFoundException();

            // Return the service.
            return service;
        }

        /// <summary>
        /// Lookup a gaming service by ID.
        /// </summary>
        /// <param name="serviceId">Target gaming service ID.</param>
        /// <returns>Matching gaming service.</returns>
        public async Task<GamingServiceDocument> GetGamingServiceById(string serviceId)
        {
            // Find the first gaming service which matches the filter.
            var service = await _gamingServiceCollection
                .Find(gs => gs.Id == serviceId)
                .FirstOrDefaultAsync();

            // Throw an exception if the service could not be found.
            if (service == null) throw new ServiceNotFoundException();

            // Return the service
            return service;
        }

        /// <summary>
        /// Retrieve a list of venues that a manager has management access to.
        /// </summary>
        /// <param name="managerId">Manager ID</param>
        /// <returns>List of Manager venue documents</returns>
        public async Task<List<ManagerVenueDocument>> GetManagerVenues(string managerId)
        {
            // Perform an aggregation on the manager collection.
            // Lookup venue Id list and populate with venue information.
            // Project out the id field, and keep the venues field.
            var result = _managerCollection.Aggregate()
                .Match(m => m.Id.Equals(managerId))
                .Lookup("venue", "venueIds", "_id", "venues")
                .Project<ManagerVenueAggregation>(new BsonDocument { { "_id", 0 }, { "venues", 1 } });

            // Select the venues from the first result.
            var firstResult = result.First().venues;

            // Perform further lookups for active service
            var managerVenues = new List<ManagerVenueDocument>();

            // Populate managerVenues with the venues from the first aggregation.
            firstResult.ForEach(v => managerVenues.Add(new ManagerVenueDocument(v)));

            // Populate each managervenuedocument.areas.activeService by performing a round trip
            // FIXME This is awful. Figure out an aggregation pipeline.
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

            // Return the venues.
            return managerVenues;
        }

        /// <summary>
        /// Determine if a manager has access to a venue.
        /// </summary>
        /// <param name="managerId">Manager ID</param>
        /// <param name="venueId">Target venue ID</param>
        /// <returns>True if the manager has access, else false.</returns>
        public async Task<bool> ManagerCanAccessVenue(string managerId, string venueId)
        {
            // Pull manager information
            var manager = await this.GetManagerById(managerId);

            // Determine if the venueId is in the manager's list of venues.
            return manager.VenueIds.Find(v => v.Equals(new ObjectId(venueId))) != null;
        }

        /// <summary>
        /// Delete a patron from a dining check-in.
        /// </summary>
        /// <param name="serviceId">Target service ID</param>
        /// <param name="tableId">Target table ID</param>
        /// <param name="checkInId">Target check-in ID</param>
        /// <param name="patronId">Target patron ID</param>
        public async Task DeleteDiningPatron(string serviceId, string tableId, string checkInId, string patronId)
        {
            // Lookup the dining service.
            var service = await this.GetDiningServiceById(serviceId);

            // Lookup the table.
            var table = service.Sittings.Find(s => s.Id.Equals(tableId));
            // Throw an exception if the table doesn't exist.
            if (table == null) throw new TableNotFoundException();

            // Lookup the check-in.
            var checkIn = table.CheckIns.Find(ci => ci.Id.Equals(checkInId));
            // Throw an exception if the check-in doesn't exist.
            if (checkIn == null) throw new CheckInNotFoundExcption();

            // Lookup the patron.
            var patron = checkIn.People.Find(p => p.Id.Equals(patronId));
            // Throw an exception if the patron doesn't exist.
            if (patron == null) throw new PatronNotFoundException();

            // If the table only has one check-in and the check-in only has 1 patron...
            if (table.CheckIns.Count == 1 && checkIn.People.Count == 1)
            {
                // Build a filter to match the dining service against the service Id
                var filter = Builders<DiningServiceDocument>.Filter.Eq(ds => ds.Id, serviceId);

                // Build a pull request to remove the sitting from the service
                var pull = Builders<DiningServiceDocument>.Update.PullFilter(ds => ds.Sittings, s => s.Id.Equals(tableId));

                // Delete the sitting
                await _diningServiceCollection.UpdateOneAsync(filter, pull);
            }
            else if (checkIn.People.Count == 1)
            {
                // Delete the check-in since there would be no more patrons in it.

                // Build a filter to match the dining service to specified serviceId.
                var filter = Builders<DiningServiceDocument>.Filter.Eq(ds => ds.Id, serviceId);

                // Build a pull filter to remove the specified checkIn
                var pull = Builders<DiningServiceDocument>.Update
                    .PullFilter("sittings.$[sitting].checkIns", Builders<CheckInDocument>.Filter.Eq(ci => ci.Id, checkInId));

                // Build array filters to select the sitting with matching tableId.
                var arrayFilters = new List<ArrayFilterDefinition> {
                    new BsonDocumentArrayFilterDefinition<DiningServiceDocument>(
                        new BsonDocument("sitting._id", new ObjectId(tableId))
                    )
                };
                var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters };

                // Delete the check in.
                await _diningServiceCollection.UpdateOneAsync(filter, pull, updateOptions);
            }
            else
            {
                // Delete only the patron.

                // Build a filter to match the dining service to the specified serviceId.
                var filter = Builders<DiningServiceDocument>.Filter.Eq(ds => ds.Id, serviceId);

                // Build a pull filter to remove the patron from the checkIn.
                var pull = Builders<DiningServiceDocument>.Update.PullFilter(
                    "sittings.$[sitting].checkIns.$[checkIn].people",
                    Builders<DiningPatronDocument>.Filter.Eq(p => p.Id, patronId)
                );

                // Build array filters to select the specified sitting and check-in.
                var arrayFilters = new List<ArrayFilterDefinition> {
                    new BsonDocumentArrayFilterDefinition<DiningServiceDocument>(
                        new BsonDocument("sitting._id", new ObjectId(tableId))
                    ),
                    new BsonDocumentArrayFilterDefinition<DiningServiceDocument>(
                        new BsonDocument("checkIn._id", new ObjectId(checkInId))
                    )
                };
                var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters };

                // Perform the pull filter update
                await _diningServiceCollection.UpdateOneAsync(filter, pull, updateOptions);
            }
        }

        /// <summary>
        /// Update a dining patron with new information.
        /// </summary>
        /// <param name="serviceId">Target service ID</param>
        /// <param name="tableId">Target table ID</param>
        /// <param name="checkInId">Target check-in ID</param>
        /// <param name="patronId">Target patron ID</param>
        /// <param name="patron">New patron information</param>
        public async Task UpdateDiningPatron(string serviceId, string tableId, string checkInId, string patronId, DiningPatronDocument patron)
        {
            // Build array filters to match the table, checkIn and patron.
            var arrayFilter = new List<ArrayFilterDefinition>() {
                new BsonDocumentArrayFilterDefinition<PublicVenueDocument>
                (
                        new BsonDocument("table._id", new ObjectId(tableId))
                ),
                new BsonDocumentArrayFilterDefinition<PublicVenueDocument>
                (
                        new BsonDocument("checkIn._id", new ObjectId(checkInId))
                ),
                new BsonDocumentArrayFilterDefinition<PublicVenueDocument>
                (
                        new BsonDocument("patron._id", new ObjectId(patronId))
                )
            };

            // Perform the update, constructing the
            await _diningServiceCollection.UpdateOneAsync(
                // Filter definition
                Builders<DiningServiceDocument>.Filter.Eq(ds => ds.Id, serviceId),
                // Update definition
                Builders<DiningServiceDocument>.Update.Set("sittings.$[table].checkIns.$[checkIn].people.$[patron]", patron),
                // Array filters
                new UpdateOptions { ArrayFilters = arrayFilter }
            );
        }

        /// <summary>
        /// Move a dining group from one table to another, creating the table if necessary and cleaning up.
        /// </summary>
        /// <param name="serviceId">Target service ID</param>
        /// <param name="tableId">Target table ID</param>
        /// <param name="checkInId">Target check-in ID</param>
        /// <param name="newTableNumber">New table number the group is being moved to</param>
        /// <returns>ID of the table that the check-in is moved to.</returns>
        /// TODO See if this function can be cleaned up or made smaller.
        public async Task<string> MoveDiningGroup(string serviceId, string tableId, string checkInId, string newTableNumber)
        {
            // Pull the service information.
            var service = await this.GetDiningServiceById(serviceId);

            // Find the table which contains the checkIn.
            var oldTable = service.Sittings.Find(s => s.Id.Equals(tableId));
            // Throw an exception if the table does not exist.
            if (oldTable == null) throw new TableNotFoundException();

            // Find the checkIn.
            var checkIn = oldTable.CheckIns.Find(ci => ci.Id.Equals(checkInId));
            // Throw an exception if the check-in does not exist.
            if (checkIn == null) throw new CheckInNotFoundExcption();

            // Find any other tables active in the service with the same table number.
            var matchingTable = service.Sittings.Find(s => s.TableNumber == newTableNumber && s.IsActive);

            if (matchingTable == null)
            {
                // No table matches, so a new sitting needs to be created.
                var newSitting = new SittingDocument
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    TableNumber = newTableNumber,
                    CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    IsActive = true,
                    CheckIns = new List<CheckInDocument> { checkIn }
                };

                // Insert the sitting
                await _diningServiceCollection.UpdateOneAsync(
                    ds => ds.Id.Equals(serviceId),
                    Builders<DiningServiceDocument>.Update.Push(ds => ds.Sittings, newSitting)
                );

                // If the old table only had one check in, delete the table.
                if (oldTable.CheckIns.Count == 1)
                {
                    await _diningServiceCollection.UpdateOneAsync(
                        // Filter
                        ds => ds.Id.Equals(serviceId),
                        // Pull filter
                        Builders<DiningServiceDocument>.Update.PullFilter(
                            ds => ds.Sittings,
                            s => s.Id.Equals(oldTable.Id)
                        )
                    );
                }
                else
                {
                    // Remove the checkIn from the old sitting
                    var arrayFilters = new List<ArrayFilterDefinition> {
                        new BsonDocumentArrayFilterDefinition<DiningServiceDocument>(
                            new BsonDocument("sitting._id", new ObjectId(oldTable.Id))
                        )
                    };

                    await _diningServiceCollection.UpdateOneAsync(
                        ds => ds.Id.Equals(serviceId),
                        Builders<DiningServiceDocument>.Update.PullFilter(
                                "sittings.$[sitting].checkIns",
                                Builders<CheckInDocument>.Filter.Eq(ci => ci.Id, checkIn.Id)
                        ),
                        new UpdateOptions { ArrayFilters = arrayFilters }
                    );
                }

                return newSitting.Id;
            }
            else
            {
                if (matchingTable.Id == tableId) throw new MatchingTableException();

                var arrayFilters = new List<ArrayFilterDefinition> {
                    new BsonDocumentArrayFilterDefinition<DiningServiceDocument>(
                        new BsonDocument("table._id", new ObjectId(matchingTable.Id))
                    )
                };

                // Delete the old table
                await _diningServiceCollection.UpdateOneAsync(
                    d => d.Id.Equals(serviceId),
                    Builders<DiningServiceDocument>.Update
                        .PullFilter(ds => ds.Sittings, c => c.Id.Equals(oldTable.Id))
                );

                // Push info to the new table
                await _diningServiceCollection.UpdateOneAsync(
                    d => d.Id.Equals(serviceId),
                    Builders<DiningServiceDocument>.Update
                        .PushEach("sittings.$[table].checkIns", oldTable.CheckIns),
                    new UpdateOptions { ArrayFilters = arrayFilters }
                );

                return matchingTable.Id;
            }
        }

        /// <summary>
        /// Move a dining table to a new table number.
        /// </summary>
        /// <param name="serviceId">Target service ID</param>
        /// <param name="tableId">Target table ID</param>
        /// <param name="newTableNumber">New table number</param>
        /// <returns>ID of new table number. May be an existing table.</returns>
        public async Task<string> MoveDiningTable(string serviceId, string tableId, string newTableNumber)
        {
            // Pull the service information.
            var service = await this.GetDiningServiceById(serviceId);

            // Find the table which is being moved.
            var oldTable = service.Sittings.Find(s => s.Id.Equals(tableId));
            // Throw an exception if the table doesn't exist.
            if (oldTable == null) throw new TableNotFoundException();

            // Find any other tables active in the service with the same table number
            var matchingTable = service.Sittings.Find(s => s.TableNumber == newTableNumber && s.IsActive);

            if (matchingTable == null)
            {
                // No table matches; only update the table number.
                var arrayFilter = new List<ArrayFilterDefinition>{
                    new BsonDocumentArrayFilterDefinition<SittingDocument>(
                        new BsonDocument( "table._id", new ObjectId(tableId) )
                    )
                };

                // Update the sitting with the new table number.
                await _diningServiceCollection.UpdateOneAsync(
                    d => d.Id.Equals(serviceId),
                    Builders<DiningServiceDocument>.Update.Set("sittings.$[table].tableNumber", newTableNumber),
                    new UpdateOptions { ArrayFilters = arrayFilter }
                );

                return tableId;
            }
            else
            {
                // Throw an exception if the request attempts to move to the same table.
                if (matchingTable.Id == tableId) throw new MatchingTableException();

                var arrayFilters = new List<ArrayFilterDefinition> {
                    new BsonDocumentArrayFilterDefinition<DiningServiceDocument>(
                        new BsonDocument("table._id", new ObjectId(matchingTable.Id))
                    )
                };

                // Delete the old table
                await _diningServiceCollection.UpdateOneAsync(
                    d => d.Id.Equals(serviceId),
                    Builders<DiningServiceDocument>.Update
                        .PullFilter(ds => ds.Sittings, c => c.Id.Equals(oldTable.Id))
                );

                // Push info to the new table
                await _diningServiceCollection.UpdateOneAsync(
                    d => d.Id.Equals(serviceId),
                    Builders<DiningServiceDocument>.Update
                        .PushEach("sittings.$[table].checkIns", oldTable.CheckIns),
                    new UpdateOptions { ArrayFilters = arrayFilters }
                );

                return matchingTable.Id;
            }
        }

        /// <summary>
        /// Close a table in a given dining service, marking at as no longer active and unmodifiable
        /// </summary>
        /// <param name="serviceId">Target dining service ID</param>
        /// <param name="tableId">Target table ID</param>
        public async Task CloseDiningTable(string serviceId, string tableId)
        {
            // Construct array filters to match the specified table ID.
            var arrayFilters = new List<ArrayFilterDefinition> {
                new BsonDocumentArrayFilterDefinition<DiningServiceDocument>(
                    new BsonDocument("table._id", new ObjectId(tableId))
                )
            };

            // Use the array filters to set (isActive = false) on the target table.
            await _diningServiceCollection.UpdateOneAsync(
                // Filter definition
                s => s.Id.Equals(serviceId),
                // Update definition
                Builders<DiningServiceDocument>.Update.Set("sittings.$[table].isActive", false),
                // Update options (array filters)
                new UpdateOptions { ArrayFilters = arrayFilters }
            );
        }

        /// <summary>
        /// Delete a gaming patron from a gaming service.
        /// </summary>
        /// <param name="serviceId">Target service ID</param>
        /// <param name="patronId">Target patron ID</param>
        public async Task DeleteGamingPatron(string serviceId, string patronId)
        {
            // Pull patron from gamingservice.patrons where patron.id = patronId.
            await _gamingServiceCollection.UpdateOneAsync(
                // Filter definition
                gs => gs.Id.Equals(serviceId),
                // Update (PullFilter) definition
                Builders<GamingServiceDocument>.Update.PullFilter(
                    gs => gs.Patrons,
                    p => p.Id.Equals(patronId)
                )
            );
        }

        /// <summary>
        /// Update a gaming patron in a given gaming service with new information.
        /// </summary>
        /// <param name="serviceId">Target service ID</param>
        /// <param name="patronId">Target patron ID</param>
        /// <param name="update">New patron information</param>
        public async Task UpdateGamingPatron(string serviceId, string patronId, GamingPatronUpdateRequest update)
        {
            // Build array filters to search for the specified patron in the gaming service.
            var arrayFilters = new List<ArrayFilterDefinition> {
                new BsonDocumentArrayFilterDefinition<GamingServiceDocument> (
                    new BsonDocument("patron._id", new ObjectId(patronId))
                )
            };

            // Update the patron's first name, last name and phone number.
            await _gamingServiceCollection.UpdateOneAsync(
                // Filter definition.
                gs => gs.Id.Equals(serviceId),
                // Update definitions to set firstName, lastName and phoneNumber.
                Builders<GamingServiceDocument>.Update
                    .Set("patrons.$[patron].firstName", update.FirstName)
                    .Set("patrons.$[patron].lastName", update.LastName)
                    .Set("patrons.$[patron].phoneNumber", update.PhoneNumber),
                // Use array filters to match the [patron] field.
                new UpdateOptions { ArrayFilters = arrayFilters }
            );
        }

        /// <summary>
        /// Checkout a gaming patron from a gaming service. Marking them as no longer active.
        /// </summary>
        /// <param name="serviceId">Target service ID</param>
        /// <param name="patronId">Target patron ID</param>
        public async Task CheckOutGamingPatron(string serviceId, string patronId)
        {
            // Build array filters to match the patron to the specified patronId.
            var arrayFilters = new List<ArrayFilterDefinition> {
                new BsonDocumentArrayFilterDefinition<GamingServiceDocument>(
                    new BsonDocument("patron._id", new ObjectId(patronId))
                )
            };

            // Perform the update of the patron's status and checkOutTime
            await _gamingServiceCollection.UpdateOneAsync(
                // Filter definition.
                gs => gs.Id.Equals(serviceId),
                // Update definition to set [patron].isActive and [patron].checkOutTime.
                Builders<GamingServiceDocument>.Update
                    .Set("patrons.$[patron].isActive", false)
                    .Set("patrons.$[patron].checkOutTime", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
                // Array filters which match [patron].
                new UpdateOptions { ArrayFilters = arrayFilters }
            );
        }

        /// <summary>
        /// Attempt to start a new dining service in a specified venue area.
        /// </summary>
        /// <param name="venueId">Target venue ID</param>
        /// <param name="areaId">Target area ID</param>
        /// <returns>A newly created dining service</returns>
        public async Task<DiningServiceDocument> StartDiningService(string venueId, string areaId)
        {
            // Lookup venue by Id.
            var venue = await this.GetVenueById(venueId);

            // Find the target area within the venue.
            var area = venue.DiningAreas.Find(a => a.Id.Equals(areaId));

            // Throw an exception if the area does not exist.
            if (area == null) throw new AreaNotFoundException();

            // Throw an exception if the area has an active service already.
            if (area.IsOpen) throw new AreaHasActiveServiceException();

            // Create a new active dining service.
            var newDiningService = new DiningServiceDocument
            {
                Id = ObjectId.GenerateNewId().ToString(),
                OpenedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                IsActive = true,
                ServiceType = "DINING",
                AreaId = areaId,
                VenueId = venueId,
                Sittings = new List<SittingDocument>()
            };

            // TODO The Insert and Update should be done in a transaction to reduce errors.
            // Save the dining service.
            await _diningServiceCollection.InsertOneAsync(newDiningService);

            // Update the venue area to reflect that it is open, and has a new activeService
            await _venueCollection.UpdateOneAsync(
                // Filter definition
                v => v.Id.Equals(venueId),
                // Update definition to set [area].isOpen and [area].activeService
                Builders<PublicVenueDocument>.Update
                    .Set("diningAreas.$[area].isOpen", true)
                    .Set("diningAreas.$[area].activeService", newDiningService.Id),
                // Build array filters to match [area].
                new UpdateOptions
                {
                    ArrayFilters = new List<ArrayFilterDefinition>{
                    new BsonDocumentArrayFilterDefinition<PublicVenueDocument>
                        (
                            new BsonDocument{
                                { "area._id", new ObjectId(areaId) }
                            }
                        )
                    }
                }
            );

            // Return the newly created dining service.
            return newDiningService;
        }

        /// <summary>
        /// Start a new gaming service in a target venue area.
        /// </summary>
        /// <param name="venueId">Target venue ID</param>
        /// <param name="areaId">Target area ID</param>
        /// <returns>Newly created gaming service document</returns>
        public async Task<GamingServiceDocument> StartGamingService(string venueId, string areaId)
        {
            // Lookup venue by Id.
            var venue = await this.GetVenueById(venueId);

            // Find the target area by Id within the venue.
            var area = venue.GamingAreas.Find(ga => ga.Id.Equals(areaId));

            // Throw an exception if the area doesn't exist.
            if (area == null) throw new AreaNotFoundException();

            // Throw an exception if the area has an active service.
            if (area.IsOpen) throw new AreaHasActiveServiceException();

            // Create a new active gaming service.
            var newGamingService = new GamingServiceDocument
            {
                Id = ObjectId.GenerateNewId().ToString(),
                OpenedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                IsActive = true,
                ServiceType = "GAMING",
                AreaId = areaId,
                VenueId = venueId,
                Patrons = new List<GamingPatronDocument>()
            };

            // TODO Transaction
            // Save the gaming service
            await _gamingServiceCollection.InsertOneAsync(newGamingService);

            // Update the venue area to reflect that it is open, and has a new activeService
            await _venueCollection.UpdateOneAsync(
                // Filter definition to match the venueId
                v => v.Id.Equals(venueId),
                // Update definition to set [area].isOpen and [area].activeService.
                Builders<PublicVenueDocument>.Update
                    .Set("gamingAreas.$[area].isOpen", true)
                    .Set("gamingAreas.$[area].activeService", newGamingService.Id),
                // Build array filters to match [area].
                new UpdateOptions
                {
                    ArrayFilters = new List<ArrayFilterDefinition>{
                    new BsonDocumentArrayFilterDefinition<PublicVenueDocument>
                        (
                            new BsonDocument{
                                { "area._id", new ObjectId(areaId) }
                            }
                        )
                    }
                }
            );

            return newGamingService;
        }

        /// <summary>
        /// Stop the active dinign service in a given venue area.
        /// </summary>
        /// <param name="venueId">Target venue Id</param>
        /// <param name="areaId">Target area Id</param>
        public async Task StopDiningService(string venueId, string areaId)
        {
            // Lookup venue information by Id.
            var venue = await this.GetVenueById(venueId);

            // Find the target area by ID within the venue.
            var area = venue.DiningAreas.Find(a => a.Id.Equals(areaId));

            // Throw an exception if teh area doesn't exist.
            if (area == null) throw new AreaNotFoundException();

            // Throw an exception if the area has no active service.
            if (!area.IsOpen) throw new AreaHasNoActiveServiceException();

            // Pull information about the active service.
            var activeService = await this.GetDiningServiceById(area.ActiveService);

            // Throw an exception if the activeService is not active. This is a strange error.
            if (!activeService.IsActive) throw new AreaHasNoActiveServiceException();

            // Create array filters for venue update to match [area].
            var arrayFilter = new List<ArrayFilterDefinition>() {
                new BsonDocumentArrayFilterDefinition<PublicVenueDocument>
                (
                    new BsonDocument
                    {
                        { "area._id", new ObjectId(areaId) }
                    }
                )
            };

            // TODO Put these two updates into a transaction.
            // Update the venue to set venue.[area].isOpen = false.
            _venueCollection.UpdateOne(
                v => v.Id.Equals(venueId),
                Builders<PublicVenueDocument>.Update.Set("diningAreas.$[area].isOpen", false),
                new UpdateOptions { ArrayFilters = arrayFilter }
            );

            // Update the service to set isActive = true, closedAt = now.
            _diningServiceCollection.UpdateOne(
                ds => ds.Id.Equals(activeService.Id),
                Builders<DiningServiceDocument>.Update
                    .Set(ds => ds.IsActive, false)
                    .Set(ds => ds.ClosedAt, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            );
        }

        /// <summary>
        /// Stop the active gaming service in a target venue area.
        /// </summary>
        /// <param name="venueId">Target venue ID</param>
        /// <param name="areaId">Target area ID</param>
        public async Task StopGamingService(string venueId, string areaId)
        {
            // Lookup venue information by ID.
            var venue = await this.GetVenueById(venueId);

            // Find the target venue by ID in the venue areas.
            var area = venue.GamingAreas.Find(a => a.Id.Equals(areaId));

            // Throw an exception if the area does not exist.
            if (area == null) throw new AreaNotFoundException();

            // Throw an exception if the area doesn't have an active service.
            if (!area.IsOpen) throw new AreaHasNoActiveServiceException();

            // Lookup the active service information.
            var activeService = await this.GetGamingServiceById(area.ActiveService);

            // Throw an exception if the active service is not active. This is a strange error.
            if (!activeService.IsActive) throw new AreaHasNoActiveServiceException();

            // Create array filters for venue update to match [area].
            var arrayFilter = new List<ArrayFilterDefinition>() {
                new BsonDocumentArrayFilterDefinition<PublicVenueDocument>
                (
                    new BsonDocument
                    {
                        { "area._id", new ObjectId(areaId) }
                    }
                )
            };

            // TODO Put these two updates into a transaction.
            // Update the venue to set venue.[area].isOpen = false.
            _venueCollection.UpdateOne(
                v => v.Id.Equals(venueId),
                Builders<PublicVenueDocument>.Update.Set("gamingAreas.$[area].isOpen", false),
                new UpdateOptions { ArrayFilters = arrayFilter }
            );

            // Update the service to set isActive = false, closedAt = now.
            _gamingServiceCollection.UpdateOne(
                ds => ds.Id.Equals(activeService.Id),
                Builders<GamingServiceDocument>.Update
                    .Set(ds => ds.IsActive, false)
                    .Set(ds => ds.ClosedAt, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            );
        }

        /// <summary>
        /// Determine if a manager can access a given service.
        /// </summary>
        /// <param name="managerId">Manager ID</param>
        /// <param name="serviceId">Target service ID</param>
        /// <returns>True if the manager has access to the service, else false</returns>
        public async Task<bool> ManagerCanAccessService(string managerId, string serviceId)
        {
            // Retrieve a venue ID from the service collection using a projection.
            // Work with BSON documents as we cannot guarantee the service type (Dining/Gaming/etc).
            var venueLookup = await _serviceCollection.Find(
                Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(serviceId))
            ).Project(Builders<BsonDocument>.Projection.Include("venueId")).FirstOrDefaultAsync();

            // Convert the venueId bson field into an objectId.
            ObjectId venueId = venueLookup["venueId"].AsObjectId;

            // Return the default operation for referencing manager and venue
            return await this.ManagerCanAccessVenue(managerId, venueId.ToString());
        }

        /// <summary>
        /// Search for a marketing user by a given email address. Filter critera also states
        /// that the user must be subscribed to marketing emails. If no users are found matching
        /// the email, throw an error.
        /// </summary>
        /// <param name="email">Email address of user</param>
        /// <returns>A user</returns>
        public async Task<MarketingUser> GetActiveMarketingUserByEmail(string email)
        {
            // Look for a user with matching email and subscribed = true.
            var user = await _marketingUserCollection.Find(
                Builders<MarketingUser>.Filter.Eq(mu => mu.Email, email) &
                Builders<MarketingUser>.Filter.Eq(mu => mu.Subscribed, true)
            ).FirstOrDefaultAsync();

            // Throw an exception if the marketing user doesn't exist.
            if (user == null) throw new MarketingUserNotFoundException();

            // Return the marketing user
            return user;
        }

        /// <summary>
        /// Change the value of a marketing users' subscription status. Lookup the user
        /// by their unique Id (bsonid).
        /// </summary>
        /// <param name="id">Unique ID of marketing user</param>
        /// <param name="isSubscribed">Subscription status. True = subscribed</param>
        public async Task SetMarketingUserSubscription(string id, bool isSubscribed)
        {
            await _marketingUserCollection.UpdateOneAsync(
                Builders<MarketingUser>.Filter.Eq(mu => mu.Id, id),
                Builders<MarketingUser>.Update.Set(mu => mu.Subscribed, isSubscribed)
            );
        }

        /// <summary>
        /// Create a new marketing user from name and email address. Additional sets the user
        /// to automatically recieve marketing emails, until they decide to unsubscribe.
        /// </summary>
        /// <param name="name">User name</param>
        /// <param name="email">Email address</param>
        /// <returns>The new inserted marketing user</returns>
        public async Task<MarketingUser> CreateMarketingUser(string name, string email)
        {
            var mUser = new MarketingUser
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = name,
                Email = email,
                Subscribed = true,
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                UnsubscribedAt = -1,
            };

            // Insert the user
            await _marketingUserCollection.InsertOneAsync(mUser);

            // Return the newly created user
            return mUser;
        }

        /// <summary>
        /// Create an unsubscribe link for a given marketing user.
        /// Accessing the link in the browser will result in the user being unsubscribed
        /// from marketing emails.
        /// </summary>
        /// <param name="mUser">Marketing for which to create link</param>
        /// <returns>Link ID</returns>
        public async Task<string> CreateMarketingUserUnsubscribeLink(MarketingUser mUser)
        {
            var mUnsub = new MarketingUnsubscribe
            {
                Id = ObjectId.GenerateNewId().ToString(),
                MarketingUserId = mUser.Id,
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                IsUsed = false,
                UsedAt = -1,
            };

            await _marketingUnsubscribeCollection.InsertOneAsync(mUnsub);

            return mUnsub.Id;
        }

        /// <summary>
        /// Using a link, unsubscribe a user from marketing communications.
        /// </summary>
        /// <param name="unsubscribeId">Unsubscription link ID</param>
        public async Task UnsubscribeFromMarketing(string unsubscribeId)
        {
            // Create a transaction session.
            using (var session = _client.StartSession())
            {
                await session.WithTransactionAsync(
                    async (s, ct) =>
                    {
                        // Lookup the unsubscription link.
                        var mUnsub = await _marketingUnsubscribeCollection.Find(m => m.Id == unsubscribeId).FirstOrDefaultAsync();

                        // Throw an error if no marketing unsubscription matches the link.
                        if (mUnsub == null) throw new MarketingUnsubscribeNotFound();

                        // Throw an exception if the link has already been used.
                        if (mUnsub.IsUsed) throw new MarketingUnsubscribeAlreadyUsed();

                        // Update the user
                        await _marketingUserCollection.UpdateOneAsync(
                            s,
                            Builders<MarketingUser>.Filter.Eq(m => m.Id, mUnsub.MarketingUserId),
                            Builders<MarketingUser>.Update
                                .Set(m => m.Subscribed, false)
                                .Set(m => m.UnsubscribedAt, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
                            cancellationToken: ct
                        );

                        // Update the marketing subscription
                        await _marketingUnsubscribeCollection.UpdateOneAsync(
                            s,
                            Builders<MarketingUnsubscribe>.Filter.Eq(m => m.Id, unsubscribeId),
                            Builders<MarketingUnsubscribe>.Update
                                .Set(m => m.IsUsed, true)
                                .Set(m => m.UsedAt, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
                            cancellationToken: ct
                        );

                        // Return any value else the compiler complains
                        return "Complete";
                    }
                );
            }
        }
    }
}
