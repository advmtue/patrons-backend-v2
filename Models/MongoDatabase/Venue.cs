using System.Collections.Generic;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace patrons_web_api.Models.MongoDatabase
{
    public abstract class VenueBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

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
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("defaultAreaId")]
        public string DefaultAreaId { get; set; }
    }

    public class PublicVenueDocument : VenueBase
    {
        [BsonElement("diningAreas")]
        public List<AreaDocument> DiningAreas { get; set; }

        [BsonElement("gamingAreas")]
        public List<AreaDocument> GamingAreas { get; set; }
    }

    public class ManagerVenueDocument : VenueBase
    {
        [BsonElement("diningAreas")]
        public List<DiningAreaDocument> DiningAreas { get; set; }

        [BsonElement("gamingAreas")]
        public List<GamingAreaDocument> GamingAreas { get; set; }

        public ManagerVenueDocument(PublicVenueDocument old)
        {
            // Copy fields
            Id = old.Id;
            VenueId = old.VenueId;
            Name = old.Name;
            ByLine = old.ByLine;
            LogoImgSrc = old.LogoImgSrc;
            DefaultAreaId = old.DefaultAreaId;

            // Create space for dining area and gaming area documents
            // We will not have the information to fully populate these at creation time, probably
            DiningAreas = new List<DiningAreaDocument>();
            GamingAreas = new List<GamingAreaDocument>();

            // Add area information
            old.DiningAreas.ForEach(da => { DiningAreas.Add(new DiningAreaDocument(da)); });
            old.GamingAreas.ForEach(ga => { GamingAreas.Add(new GamingAreaDocument(ga)); });
        }
    }
}