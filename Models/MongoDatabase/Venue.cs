using System.Collections.Generic;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace patrons_web_api.Models.MongoDatabase
{
    public class VenueBase
    {
        // URL Mapping
        [BsonElement("venueId")]
        public string VenueId { get; set; }

        // Display information
        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("byLine")]
        public string ByLine { get; set; }

        [BsonElement("logoImgSrc")]
        public string LogoImgSrc { get; set; }


        // Useful for venues which want to to have URL list patrons.at/venue and not patrons.at/venue/area
        [BsonElement("defaultAreaIndex")]
        public int DefaultAreaIndex { get; set; }
    }

    public class VenueDocument : VenueBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("areas")]
        public List<AreaDocument> Areas { get; set; }
    }

    public class Venue : VenueBase
    {
        [BsonElement("areas")]
        public List<AreaBase> Areas { get; set; }
    }
}