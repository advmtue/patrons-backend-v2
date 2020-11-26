using System;
using System.Collections.Generic;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Threading.Tasks;

using patrons_web_api.Models.MongoDatabase;
using patrons_web_api.Models.Transfer.Request;
using patrons_web_api.Models.Transfer.Response;

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
        private IMongoCollection<VenueDocument> _venueCollection;
        private IMongoCollection<BsonDocument> _venueAggregationCollection;
        private IMongoCollection<ServiceDocument> _serviceCollection;
        private IMongoCollection<GamingPatronDocument> _gamingPatronCollection;
        private IMongoCollection<SittingDocument> _sittingCollection;
        private IMongoCollection<ManagerDocument> _managerCollection;
        private IMongoCollection<SessionDocument> _sessionCollection;

        public MongoDatabase(IMongoDatabaseSettings settings)
        {
            // Create client connection
            var client = new MongoClient(
                $"mongodb://{settings.Username}:{settings.Password}@{settings.Host}/{settings.AuthDatabase}"
            );

            var database = client.GetDatabase(settings.UseDatabase);

            _venueCollection = database.GetCollection<VenueDocument>("venue");
            _venueAggregationCollection = database.GetCollection<BsonDocument>("venue");
            _serviceCollection = database.GetCollection<ServiceDocument>("service");
            _gamingPatronCollection = database.GetCollection<GamingPatronDocument>("gaming_patron");
            _sittingCollection = database.GetCollection<SittingDocument>("sitting");
            _managerCollection = database.GetCollection<ManagerDocument>("manager");
            _sessionCollection = database.GetCollection<SessionDocument>("session");
        }

        public async Task<VenueDocument> getVenueInfo(string venueId)
        {
            // Todo
            var venue = await _venueCollection.Find(v => v.VenueId == venueId).FirstOrDefaultAsync();

            if (venue == null)
            {
                throw new VenueNotFoundException();
            }

            return venue;
        }

        public async Task<VenueResponse> getPatronVenueView(string venueId, string indexName = "venueId")
        {
            // TODO Find a neater way to write this aggregation pipeline
            BsonDocument matchDocument;
            if (indexName == "_id")
            {
                matchDocument = new BsonDocument { { indexName, new ObjectId(venueId) } };
            }
            else
            {
                matchDocument = new BsonDocument { { indexName, venueId } };
            }

            var pipeline = PipelineDefinition<BsonDocument, VenueResponse>.Create(new BsonDocument[] {
                new BsonDocument
                {
                    { "$match", matchDocument },
                },
                new BsonDocument {
                    {
                        "$unwind", "$areas"
                    }
                },
                new BsonDocument
                {
                    {
                        "$lookup", new BsonDocument {
                            { "from", "service" },
                            { "localField", "areas._id" },
                            { "foreignField", "areaId" },
                            { "as", "currentService" },
                        }
                    },
                },
                // TODO Filter out non-active services
                new BsonDocument
                {
                    {
                        "$unwind", new BsonDocument {
                            { "path", "$currentService" },
                            { "preserveNullAndEmptyArrays", true }
                        }
                    },
                },
                new BsonDocument
                {
                    {
                        "$addFields", new BsonDocument {
                            {
                                "areas.currentServiceType", new BsonDocument {
                                    { "$ifNull", new BsonArray { "$currentService.type", "NONE" } }
                                }
                            },
                            {
                                "areas.currentServiceId", new BsonDocument {
                                    { "$ifNull", new BsonArray { "$currentService._id", "NONE" } }
                                }
                            }
                        }
                    },
                },
                new BsonDocument
                {
                    {
                        "$project", new BsonDocument {
                            { "currentService", 0 }
                        }
                    }
                },
                new BsonDocument
                {
                    {
                        "$group", new BsonDocument {
                            { "_id", "$_id" },
                            { "a", new BsonDocument {
                                { "$push", "$areas" }
                            }},
                            { "b", new BsonDocument {
                                { "$mergeObjects", "$$ROOT"}
                            }}
                        }
                    }
                },
                new BsonDocument
                {
                    {
                        "$addFields", new BsonDocument {
                            { "b.areas", "$a" }
                        }
                    }
                },
                new BsonDocument {
                    {
                        "$replaceRoot", new BsonDocument {
                            { "newRoot", "$b" }
                        }
                    }
                }
            });

            // Perform aggregation lookup
            var info = await _venueAggregationCollection.Aggregate<VenueResponse>(pipeline).FirstAsync();

            return info;
        }

        public async Task GamingCheckIn(string venueId, string areaId, GamingCheckInRequest checkIn)
        {
            // Check that the venue exists, the area exists, the area is open, the area type is gaming
            // This will throw an error if the venue doesn't exist
            var patronVenueView = await this.getPatronVenueView(venueId, "_id");

            // Find an area that matches the requested areaId
            var area = patronVenueView.Areas.Find(a => a.Id == areaId);

            // Existence check
            if (area == null)
            {
                throw new AreaNotFoundException();
            }

            // Check that area is open
            if (!area.IsOpen)
            {
                throw new AreaIsClosedException();
            }

            // Check that the area type is gaming
            // TODO No more magic strings
            if (area.CurrentServiceType != "GAMING")
            {
                throw new WrongAreaServiceTypeException();
            }

            // Ensure that there is an active service
            if (area.CurrentServiceId == "NONE")
            {
                throw new ServiceNotFoundException();
            }

            // Create a new GamingPatron document
            GamingPatronDocument patron = new GamingPatronDocument
            {
                Id = ObjectId.GenerateNewId().ToString(),
                FirstName = checkIn.FirstName,
                LastName = checkIn.LastName,
                PhoneNumber = checkIn.PhoneNumber,
                ServiceId = area.CurrentServiceId,
                CheckInTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                CheckOutTime = -1,
                IsActive = true
            };

            // Insert into collection
            await this._gamingPatronCollection.InsertOneAsync(patron);
        }

        public async Task DiningCheckIn(string venueId, string areaId, DiningCheckInRequest checkIn)
        {
            // Check that the venue exists, the area exists, the area is open, the area type is gaming
            // This will throw an error if the venue doesn't exist
            var patronVenueView = await this.getPatronVenueView(venueId, "_id");

            // Find an area that matches the requested areaId
            var area = patronVenueView.Areas.Find(a => a.Id == areaId);

            // Existence check
            if (area == null)
            {
                throw new AreaNotFoundException();
            }

            // Check that area is open
            if (!area.IsOpen)
            {
                throw new AreaIsClosedException();
            }

            // Check that the area type is dining
            // TODO No more magic strings
            if (area.CurrentServiceType != "DINING")
            {
                throw new WrongAreaServiceTypeException();
            }

            // Ensure that there is an active service
            if (area.CurrentServiceId == "NONE")
            {
                throw new ServiceNotFoundException();
            }

            // Create a check in
            CheckInDocument checkInDocument = CheckInDocument.FromCheckInRequest(checkIn);

            var update = Builders<SittingDocument>.Update.Push("checkIns", checkInDocument);

            // Filter to match
            var filter = Builders<SittingDocument>.Filter.Eq("serviceId", area.CurrentServiceId)
                & Builders<SittingDocument>.Filter.Eq("isActive", true)
                & Builders<SittingDocument>.Filter.Eq("tableNumber", checkIn.TableNumber);

            // Perform update
            var updateInfo = _sittingCollection.UpdateOne(filter, update);

            // If no update occurred, create a new sitting
            if (updateInfo.ModifiedCount == 0)
            {
                SittingDocument newSitting = new SittingDocument
                {
                    TableNumber = checkIn.TableNumber,
                    CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    IsActive = true,
                    ServiceId = area.CurrentServiceId,
                    CheckIns = new List<CheckInDocument> { checkInDocument }
                };

                _sittingCollection.InsertOne(newSitting);
            }
        }

        public async Task<SittingDocument> FindActiveSittingByServiceAndTable(string serviceId, string tableNumber)
        {
            var sitting = await _sittingCollection
                .Find(sitting => sitting.ServiceId == serviceId && sitting.TableNumber == tableNumber)
                .FirstOrDefaultAsync();

            if (sitting == null)
            {
                throw new SittingNotFoundException();
            }

            return sitting;
        }

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
            var session = await _sessionCollection.Find(s => s.SessionId == sessionId).FirstOrDefaultAsync();

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
    }
}