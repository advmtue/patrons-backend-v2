using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using patrons_web_api.Database;

using MongoDB.Bson;

using patrons_web_api.Controllers;
using patrons_web_api.Models.MongoDatabase;

namespace patrons_web_api.Services
{
    public interface IPatronService
    {
        Task SubmitGamingCheckIn(string venueId, string areaId, GamingCheckInRequest checkIn);
        Task SubmitDiningCheckIn(string venueId, string areaId, DiningCheckInRequest checkIn);
    }

    public class PatronService : IPatronService
    {
        private IPatronsDatabase _database;

        public PatronService(IPatronsDatabase database)
        {
            _database = database;
        }

        /// <summary>
        /// Patron check-in to a gaming service.
        /// </summary>
        /// <param name="venueId">Target venue ID</param>
        /// <param name="areaId">Target area ID</param>
        /// <param name="checkIn">Check-in information</param>
        public async Task SubmitGamingCheckIn(string venueId, string areaId, GamingCheckInRequest checkIn)
        {
            // Check that the venue exists, the area exists, the area is open, and the area type is gaming.
            // This will throw an error if the venue doesn't exist.
            var venue = await _database.GetVenueById(venueId);

            // Find an area that matches the requested areaId
            var area = venue.GamingAreas.Find(a => a.Id.Equals(areaId));

            // Throw an exception if the area does not exist.
            if (area == null) throw new AreaNotFoundException();

            // Throw an exception if the area is not open.
            if (!area.IsOpen) throw new AreaIsClosedException();

            // Throw an exception if there is no active service.
            if (area.ActiveService == "NONE") throw new ServiceNotFoundException();

            // Create a new GamingPatron document.
            GamingPatronDocument patron = new GamingPatronDocument
            {
                Id = ObjectId.GenerateNewId().ToString(),
                FirstName = checkIn.FirstName,
                LastName = checkIn.LastName,
                PhoneNumber = checkIn.PhoneNumber,
                CheckInTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                CheckOutTime = -1,
                IsActive = true
            };

            // Save the check-in.
            await _database.SaveGamingCheckIn(area.ActiveService, patron);
        }

        /// <summary>
        /// Check-in a group of patrons to a dining service.
        /// </summary>
        /// <param name="venueId">Target venue ID</param>
        /// <param name="areaId">Target area ID</param>
        /// <param name="checkIn">Check-in information</param>
        public async Task SubmitDiningCheckIn(string venueId, string areaId, DiningCheckInRequest checkIn)
        {
            // Lookup venue information.
            // This will throw an error if the venue doesn't exist.
            var venue = await _database.GetVenueById(venueId);

            // Find an area that matches the requested areaId.
            var area = venue.DiningAreas.Find(a => a.Id == areaId);

            // Throw an exception if the area doesn't exist.
            if (area == null) throw new AreaNotFoundException();

            // Throw an exception if the area isn't open.
            if (!area.IsOpen) throw new AreaIsClosedException();

            // Throw an error if there is no active service.
            if (area.ActiveService == "NONE") throw new ServiceNotFoundException();

            // Create a new check-in.
            CheckInDocument checkInDocument = new CheckInDocument
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                People = new List<DiningPatronDocument>()
            };

            // Add patrons from the request into the check-in.
            checkIn.People.ForEach(person =>
            {
                checkInDocument.People.Add(new DiningPatronDocument
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    FirstName = person.FirstName,
                    PhoneNumber = person.PhoneNumber
                });
            });

            // Save the check-in to a new table, or append to an existing table.
            await _database.CreateOrAppendDiningCheckIn(area.ActiveService, checkIn.TableNumber, checkInDocument);
        }
    }
}