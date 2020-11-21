using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

using System.Text.Json.Serialization;
using System.Collections.Generic;

using patrons_web_api.Models.MongoDatabase;

namespace patrons_web_api.Models.Transfer.Response
{
    public class VenueResponse : VenueBase
    {
        [BsonElement("areas")]
        [JsonPropertyName("areas")]
        public List<AreaResponse> Areas { get; set; }

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
    }

    public class AreaResponse : AreaBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("currentServiceType")]
        public string CurrentServiceType { get; set; }
    }
}