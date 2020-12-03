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
            var filter = Builders<PublicVenueDocument>.Filter.Eq(v => v.VenueId, urlName);

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
            var filter = Builders<GamingServiceDocument>.Filter.Eq(gs => gs.Id, serviceId);
            var update = Builders<GamingServiceDocument>.Update.Push(gs => gs.Patrons, patron);

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
            var serviceFilter = Builders<DiningServiceDocument>.Filter.Eq(ds => ds.Id, serviceId)
                & Builders<DiningServiceDocument>.Filter.Eq(ds => ds.IsActive, true);

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
            var sittingsPushUpdate = Builders<DiningServiceDocument>.Update.Push(ds => ds.Sittings, newSitting);

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
            var filter = Builders<SessionDocument>.Filter.Eq(s => s.SessionId, sessionId);

            var session = await _sessionCollection.Find(filter).FirstOrDefaultAsync();

            if (session == null)
            {
                throw new SessionNotFoundException();
            }

            return session;
        }

        public async Task<ManagerDocument> GetManagerById(string managerId)
        {
            var manager = await _managerCollection.Find(
                Builders<ManagerDocument>.Filter.Eq(m => m.Id, managerId)
            ).FirstAsync();

            if (manager == null)
            {
                throw new ManagerNotFoundException();
            }

            return manager;
        }

        public async Task ManagerUpdatePassword(string managerId, string newPassword, string newSalt)
        {
            // Filter to match manager._id with ObjectId(managerId)
            var filter = Builders<ManagerDocument>.Filter.Eq(m => m.Id, managerId);

            // Update to set password to the new value and isPasswordReset = false
            var update = Builders<ManagerDocument>.Update
                .Set(m => m.Password, newPassword)
                .Set(m => m.Salt, newSalt)
                .Set(m => m.IsPasswordReset, false);

            // Update manager document
            await _managerCollection.UpdateOneAsync(filter, update);
        }

        public async Task ManagerDeactivateSessions(string managerId)
        {
            // Filter to match session.managerId with ObjectId(managerId)
            var filter = Builders<SessionDocument>.Filter.Eq(s => s.ManagerId, managerId);

            // Update to disable the session
            var update = Builders<SessionDocument>.Update.Set(s => s.IsActive, false);

            await _sessionCollection.UpdateManyAsync(filter, update);
        }

        public async Task<DiningServiceDocument> GetDiningServiceById(string serviceId)
        {
            var filter = Builders<DiningServiceDocument>.Filter.Eq(ds => ds.Id, serviceId);
            var service = await _diningServiceCollection.Find(filter).FirstOrDefaultAsync();

            if (service == null)
            {
                throw new ServiceNotFoundException();
            }

            return service;
        }

        public async Task<GamingServiceDocument> GetGamingServiceById(string serviceId)
        {
            var filter = Builders<GamingServiceDocument>.Filter.Eq(gs => gs.Id, serviceId);
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
                .Match(m => m.Id.Equals(managerId))
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

        public async Task DeleteDiningPatron(string serviceId, string tableId, string checkInId, string patronId)
        {
            var service = await this.GetDiningServiceById(serviceId);

            // Find the table
            var table = service.Sittings.Find(s => s.Id.Equals(tableId));
            if (table == null) throw new TableNotFoundException();

            // Find the checkIn
            var checkIn = table.CheckIns.Find(ci => ci.Id.Equals(checkInId));
            if (checkIn == null) throw new CheckInNotFoundExcption();

            // Find the patron
            var patron = checkIn.People.Find(p => p.Id.Equals(patronId));
            if (patron == null) throw new PatronNotFoundException();

            if (table.CheckIns.Count == 1 && checkIn.People.Count == 1)
            {
                // Delete the sitting
                await _diningServiceCollection.UpdateOneAsync(
                    ds => ds.Id.Equals(serviceId),
                    Builders<DiningServiceDocument>.Update.PullFilter(
                        ds => ds.Sittings,
                        s => s.Id.Equals(tableId)
                    )
                );
            }
            else if (checkIn.People.Count == 1)
            {
                var arrayFilters = new List<ArrayFilterDefinition> {
                    new BsonDocumentArrayFilterDefinition<DiningServiceDocument>(
                        new BsonDocument("sitting._id", new ObjectId(tableId))
                    )
                };

                // Delete the check in
                await _diningServiceCollection.UpdateOneAsync(
                    ds => ds.Id.Equals(serviceId),
                    Builders<DiningServiceDocument>.Update.PullFilter(
                        "sittings.$[sitting].checkIns",
                        Builders<CheckInDocument>.Filter.Eq(ci => ci.Id, checkInId)
                    ),
                    new UpdateOptions { ArrayFilters = arrayFilters }
                );
            }
            else
            {
                // Delete the patron
                var arrayFilters = new List<ArrayFilterDefinition> {
                    new BsonDocumentArrayFilterDefinition<DiningServiceDocument>(
                        new BsonDocument("sitting._id", new ObjectId(tableId))
                    ),
                    new BsonDocumentArrayFilterDefinition<DiningServiceDocument>(
                        new BsonDocument("checkIn._id", new ObjectId(checkInId))
                    )
                };

                await _diningServiceCollection.UpdateOneAsync(
                    ds => ds.Id.Equals(serviceId),
                    Builders<DiningServiceDocument>.Update.PullFilter(
                        "sittings.$[sitting].checkIns.$[checkIn].people",
                        Builders<DiningPatronDocument>.Filter.Eq(p => p.Id, patronId)
                    ),
                    new UpdateOptions { ArrayFilters = arrayFilters }
                );
            }
        }

        public async Task UpdateDiningPatron(string serviceId, string tableId, string checkInId, string patronId, DiningPatronDocument patron)
        {
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

            await _diningServiceCollection.UpdateOneAsync(
                Builders<DiningServiceDocument>.Filter.Eq(ds => ds.Id, serviceId),
                Builders<DiningServiceDocument>.Update.Set("sittings.$[table].checkIns.$[checkIn].people.$[patron]", patron),
                new UpdateOptions { ArrayFilters = arrayFilter }
            );
        }

        public async Task<string> MoveDiningGroup(string serviceId, string tableId, string checkInId, string newTableNumber)
        {
            // Pull the service
            var service = await this.GetDiningServiceById(serviceId);

            // Find the table which contains the checkIn
            var oldTable = service.Sittings.Find(s => s.Id.Equals(tableId));
            if (oldTable == null) throw new TableNotFoundException();

            // Find the checkIn
            var checkIn = oldTable.CheckIns.Find(ci => ci.Id.Equals(checkInId));
            if (checkIn == null) throw new CheckInNotFoundExcption();

            // Find any other tables active in the service with the same table number
            var matchingTable = service.Sittings.Find(s => s.TableNumber == newTableNumber && s.IsActive);

            if (matchingTable == null)
            {
                // No table matches, so a new sitting needs to be pushed
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

                // If the old table only had one check in, delete it
                if (oldTable.CheckIns.Count == 1)
                {
                    await _diningServiceCollection.UpdateOneAsync(
                        ds => ds.Id.Equals(serviceId),
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

        public async Task<string> MoveDiningTable(string serviceId, string tableId, string newTableNumber)
        {
            // Pull the service
            var service = await this.GetDiningServiceById(serviceId);

            // Find the table which is being moved
            var oldTable = service.Sittings.Find(s => s.Id.Equals(tableId));
            if (oldTable == null) throw new TableNotFoundException();

            // Find any other tables active in the service with the same table number
            var matchingTable = service.Sittings.Find(s => s.TableNumber == newTableNumber && s.IsActive);

            if (matchingTable == null)
            {
                // No table matches, so we can just update the table number
                var arrayFilter = new List<ArrayFilterDefinition>{
                    new BsonDocumentArrayFilterDefinition<SittingDocument>(
                        new BsonDocument( "table._id", new ObjectId(tableId) )
                    )
                };

                // Update the sitting
                await _diningServiceCollection.UpdateOneAsync(
                    d => d.Id.Equals(serviceId),
                    Builders<DiningServiceDocument>.Update.Set("sittings.$[table].tableNumber", newTableNumber),
                    new UpdateOptions { ArrayFilters = arrayFilter }
                );

                return tableId;
            }
            else
            {
                // Table which matches tableNumber cannot match the table that is being moved
                // Ie they are the same table
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

        public async Task CloseDiningTable(string serviceId, string tableId)
        {
            var arrayFilters = new List<ArrayFilterDefinition> {
                new BsonDocumentArrayFilterDefinition<DiningServiceDocument>(
                    new BsonDocument("table._id", new ObjectId(tableId))
                )
            };

            await _diningServiceCollection.UpdateOneAsync(
                s => s.Id.Equals(serviceId),
                Builders<DiningServiceDocument>.Update.Set("sittings.$[table].isActive", false),
                new UpdateOptions { ArrayFilters = arrayFilters }
            );
        }

        public async Task DeleteGamingPatron(string serviceId, string patronId)
        {
            // Pull patron from gamingservice.patrons where patron.id = patronId
            await _gamingServiceCollection.UpdateOneAsync(
                gs => gs.Id.Equals(serviceId),
                Builders<GamingServiceDocument>.Update.PullFilter(
                    gs => gs.Patrons,
                    p => p.Id.Equals(patronId)
                )
            );
        }

        public async Task UpdateGamingPatron(string serviceId, string patronId, GamingPatronUpdateRequest update)
        {
            var arrayFilters = new List<ArrayFilterDefinition> {
                new BsonDocumentArrayFilterDefinition<GamingServiceDocument> (
                    new BsonDocument("patron._id", new ObjectId(patronId))
                )
            };

            await _gamingServiceCollection.UpdateOneAsync(
                gs => gs.Id.Equals(serviceId),
                Builders<GamingServiceDocument>.Update
                    .Set("patrons.$[patron].firstName", update.FirstName)
                    .Set("patrons.$[patron].lastName", update.LastName)
                    .Set("patrons.$[patron].phoneNumber", update.PhoneNumber),
                new UpdateOptions { ArrayFilters = arrayFilters }
            );
        }

        public async Task CheckOutGamingPatron(string serviceId, string patronId)
        {
            var arrayFilters = new List<ArrayFilterDefinition> {
                new BsonDocumentArrayFilterDefinition<GamingServiceDocument>(
                    new BsonDocument("patron._id", new ObjectId(patronId))
                )
            };

            await _gamingServiceCollection.UpdateOneAsync(
                gs => gs.Id.Equals(serviceId),
                Builders<GamingServiceDocument>.Update
                    .Set("patrons.$[patron].isActive", false)
                    .Set("patrons.$[patron].checkOutTime", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
                new UpdateOptions { ArrayFilters = arrayFilters }
            );
        }

        public async Task<DiningServiceDocument> StartDiningService(string venueId, string areaId)
        {
            // Pull venue
            var venue = await this.GetVenueById(venueId);

            // Find area
            var area = venue.DiningAreas.Find(a => a.Id.Equals(areaId));

            // Ensure area exists
            if (area == null) throw new AreaNotFoundException();

            // Ensure area has no active service
            if (area.IsOpen) throw new AreaHasActiveServiceException();

            // Create a new active dining service
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

            // Save the dining service
            await _diningServiceCollection.InsertOneAsync(newDiningService);

            // Update the venue area to reflect that it is open, and has a new activeService
            await _venueCollection.UpdateOneAsync(
                v => v.Id.Equals(venueId),
                Builders<PublicVenueDocument>.Update
                    .Set("diningAreas.$[area].isOpen", true)
                    .Set("diningAreas.$[area].activeService", newDiningService.Id),
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

            return newDiningService;
        }

        public async Task<GamingServiceDocument> StartGamingService(string venueId, string areaId)
        {
            // Pull venue
            var venue = await this.GetVenueById(venueId);

            // Find area
            var area = venue.GamingAreas.Find(ga => ga.Id.Equals(areaId));

            // Ensure area exists
            if (area == null) throw new AreaNotFoundException();

            // Ensure area has no active service
            if (area.IsOpen) throw new AreaHasActiveServiceException();

            // Create a new active dining service
            var newGamingService = new GamingServiceDocument
            {
                Id = ObjectId.GenerateNewId().ToString(),
                OpenedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                IsActive = true,
                ServiceType = "DINING",
                AreaId = areaId,
                VenueId = venueId,
                Patrons = new List<GamingPatronDocument>()
            };

            // Save the dining service
            await _gamingServiceCollection.InsertOneAsync(newGamingService);

            // Update the venue area to reflect that it is open, and has a new activeService
            await _venueCollection.UpdateOneAsync(
                v => v.Id.Equals(venueId),
                Builders<PublicVenueDocument>.Update
                    .Set("gamingAreas.$[area].isOpen", true)
                    .Set("gamingAreas.$[area].activeService", newGamingService.Id),
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

        public async Task StopDiningService(string venueId, string areaId)
        {
            // Pull venue
            var venue = await this.GetVenueById(venueId);

            // Find area
            var area = venue.DiningAreas.Find(a => a.Id.Equals(areaId));

            // Ensure area exists
            if (area == null) throw new AreaNotFoundException();

            // Ensure area has an active service
            if (!area.IsOpen) throw new AreaHasNoActiveServiceException();

            // Pull the active service
            var activeService = await this.GetDiningServiceById(area.ActiveService);

            // Confirm the service is not open
            if (!activeService.IsActive) throw new AreaHasNoActiveServiceException();

            // Create array filters for venue update
            var arrayFilter = new List<ArrayFilterDefinition>() {
                new BsonDocumentArrayFilterDefinition<PublicVenueDocument>
                (
                    new BsonDocument
                    {
                        { "area._id", new ObjectId(areaId) }
                    }
                )
            };

            // Update the venue
            _venueCollection.UpdateOne(
                v => v.Id.Equals(venueId),
                Builders<PublicVenueDocument>.Update.Set("diningAreas.$[area].isOpen", false),
                new UpdateOptions { ArrayFilters = arrayFilter }
            );

            // Update the service
            _diningServiceCollection.UpdateOne(
                ds => ds.Id.Equals(activeService.Id),
                Builders<DiningServiceDocument>.Update
                    .Set(ds => ds.IsActive, false)
                    .Set(ds => ds.ClosedAt, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            );
        }

        public async Task StopGamingService(string venueId, string areaId)
        {
            // Pull venue
            var venue = await this.GetVenueById(venueId);

            // Find area
            var area = venue.GamingAreas.Find(a => a.Id.Equals(areaId));

            // Ensure area exists
            if (area == null) throw new AreaNotFoundException();

            // Ensure area has an active service
            if (!area.IsOpen) throw new AreaHasNoActiveServiceException();

            // Pull the active service
            var activeService = await this.GetGamingServiceById(area.ActiveService);

            // Confirm the service is not open
            if (!activeService.IsActive) throw new AreaHasNoActiveServiceException();

            // Create array filters for venue update
            var arrayFilter = new List<ArrayFilterDefinition>() {
                new BsonDocumentArrayFilterDefinition<PublicVenueDocument>
                (
                    new BsonDocument
                    {
                        { "area._id", new ObjectId(areaId) }
                    }
                )
            };

            // Update the venue
            _venueCollection.UpdateOne(
                v => v.Id.Equals(venueId),
                Builders<PublicVenueDocument>.Update.Set("gamingAreas.$[area].isOpen", false),
                new UpdateOptions { ArrayFilters = arrayFilter }
            );

            // Update the service
            _gamingServiceCollection.UpdateOne(
                ds => ds.Id.Equals(activeService.Id),
                Builders<GamingServiceDocument>.Update
                    .Set(ds => ds.IsActive, false)
                    .Set(ds => ds.ClosedAt, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            );
        }

        public async Task<bool> ManagerCanAccessService(string managerId, string serviceId)
        {
            // Retrieve a venue ID from the service collection using a projection
            // Working with BSON documents as we cannot guarantee the service type (Dining/Gaming/etc)
            var venueLookup = await _serviceCollection.Find(
                Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(serviceId))
            ).Project(Builders<BsonDocument>.Projection.Include("venueId")).FirstOrDefaultAsync();

            // Convert the bson field into an objectId
            ObjectId venueId = venueLookup["venueId"].AsObjectId;

            // Return the default operation for referencing manager and venue
            return await this.ManagerCanAccessVenue(managerId, venueId.ToString());
        }

        public Task<PublicVenueDocument> GetVenueByServiceId(string serviceId)
        {
            throw new NotImplementedException();
        }
    }
}