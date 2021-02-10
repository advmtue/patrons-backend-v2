using System.Collections.Generic;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Patrons.CheckIn.API.Models.MongoDatabase
{
    public class SittingDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("tableNumber")]
        public string TableNumber { get; set; }

        [BsonElement("createdAt")]
        public long CreatedAt { get; set; }

        [BsonElement("isActive")]
        public bool IsActive { get; set; }

        [BsonElement("checkIns")]
        public List<CheckInDocument> CheckIns { get; set; }
    }
}
