using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Patrons.CheckIn.API.Models.MongoDatabase
{
    public class MarketingUnsubscribe
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("marketingUserId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string MarketingUserId { get; set; }

        [BsonElement("createdAt")]
        public long CreatedAt { get; set; }

        [BsonElement("isUsed")]
        public bool IsUsed { get; set; }

        [BsonElement("usedAt")]
        public long UsedAt { get; set; }
    }
}
