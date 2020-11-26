using System.Collections.Generic;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace patrons_web_api.Models.MongoDatabase
{
    public abstract class SittingBase
    {
        [BsonElement("tableNumber")]
        public string TableNumber { get; set; }

        [BsonElement("createdAt")]
        public long CreatedAt { get; set; }

        [BsonElement("isActive")]
        public bool IsActive { get; set; }

        [BsonElement("serviceId")]
        public string ServiceId { get; set; }
    }

    public class Sitting : SittingBase
    {
        [BsonElement("checkIns")]
        public List<CheckIn> CheckIns { get; set; }
    }

    public class SittingDocument : SittingBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("checkIns")]
        public List<CheckInDocument> CheckIns { get; set; }
    }
}