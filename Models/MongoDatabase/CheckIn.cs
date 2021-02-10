using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Patrons.CheckIn.API.Models.MongoDatabase
{
    public class CheckInDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("time")]
        public long Time { get; set; }

        [BsonElement("people")]
        public List<DiningPatronDocument> People { get; set; }
    }
}
