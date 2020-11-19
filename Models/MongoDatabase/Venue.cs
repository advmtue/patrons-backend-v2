using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace patrons_web_api.Models.MongoDatabase
{

    public class VenueSimple
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("venueId")]
        public string VenueId { get; set; }

        [BsonElement("logoImgSrc")]
        public string LogoImgSrc { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("byLine")]
        public string ByLine { get; set; }

        [BsonElement("status")]
        public string Status { get; set; }
    }

    public class Venue : VenueSimple
    {
        [BsonElement("password")]
        public string Password { get; set; }

        public VenueSimple toVenueSimple()
        {
            return new VenueSimple
            {
                Id = this.Id,
                VenueId = this.VenueId,
                LogoImgSrc = this.LogoImgSrc,
                Name = this.Name,
                ByLine = this.ByLine,
                Status = this.Status
            };
        }
    }
}