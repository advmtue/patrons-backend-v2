using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace patrons_web_api.Models.MongoDatabase
{
    public abstract class ServiceBase
    {
        // Corresponding area which the service is taking place
        [BsonElement("areaId")]
        public string AreaId { get; set; }

        // Type of service such as GAMING or DINING
        // TODO Enum or const checks
        [BsonElement("type")]
        public string Type { get; set; }

        // Open/close times
        [BsonElement("openedAt")]
        public long OpenedAt { get; set; }

        [BsonElement("closedAt")]
        public long ClosedAt { get; set; }

        // Is this the active service for the area?
        [BsonElement("isActive")]
        public bool IsActive { get; set; }
    }

    public class Service : ServiceBase
    {
        [BsonElement("sittings")]
        public List<Sitting> Sittings { get; set; }
    }

    public class ServiceDocument : ServiceBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("sittings")]
        public List<SittingDocument> Sittings { get; set; }
    }
}