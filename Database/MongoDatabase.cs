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
    public class VenueNotFoundException : Exception { }
    public class VenueHasNoActiveServiceException : Exception { }
    public class VenueIsClosedException : Exception { }

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

        public MongoDatabase(IMongoDatabaseSettings settings)
        {
            // Create client connection
            var client = new MongoClient(
                $"mongodb://{settings.Username}:{settings.Password}@{settings.Host}/{settings.AuthDatabase}"
            );

            var database = client.GetDatabase(settings.UseDatabase);

            _venueCollection = database.GetCollection<VenueDocument>("venue");
            _venueAggregationCollection = database.GetCollection<BsonDocument>("venue");
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

        public async Task<VenueResponse> getPatronVenueView(string venueId)
        {
            var pipeline = PipelineDefinition<BsonDocument, VenueResponse>.Create(new BsonDocument[] {
                new BsonDocument
                {
                    {
                        "$match",
                        new BsonDocument {
                            { "venueId", venueId }
                        }
                    },
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
                            { "as", "currentService" }
                        }
                    },
                },
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
    }
}