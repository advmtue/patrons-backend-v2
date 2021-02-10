using System.Collections.Generic;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Patrons.CheckIn.API.Models.MongoDatabase
{
    public abstract class ServiceDocumentBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // Time that the service was opened
        [BsonElement("openedAt")]
        public long OpenedAt { get; set; }

        // Time that the service was closed
        [BsonElement("closedAt")]
        public long ClosedAt { get; set; }

        // Is this the active service for the area?
        [BsonElement("isActive")]
        public bool IsActive { get; set; }

        // Type of service this area holds
        [BsonElement("serviceType")]
        public string ServiceType { get; set; }

        // Area which owns this venue
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("areaId")]
        public string AreaId { get; set; }

        // Venue which owns this service
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("venueId")]
        public string VenueId { get; set; }
    }

    public class DiningServiceDocument : ServiceDocumentBase
    {
        [BsonElement("sittings")]
        public List<SittingDocument> Sittings { get; set; }
    }

    public class GamingServiceDocument : ServiceDocumentBase
    {
        [BsonElement("patrons")]
        public List<GamingPatronDocument> Patrons { get; set; }
    }
}
