using System.Collections.Generic;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace patrons_web_api.Models.MongoDatabase
{
    public abstract class SittingBase
    {
        [BsonElement("tableNumber")]
        public string TableNumber { get; set; }

        [BsonElement("firstCheckInTime")]
        public long FirstCheckInTime { get; set; }

        [BsonElement("status")]
        public string Status { get; set; }

        [BsonElement("venueId")]
        public string VenueId { get; set; }
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