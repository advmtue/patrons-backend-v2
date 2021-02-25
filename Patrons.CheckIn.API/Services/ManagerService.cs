using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using MongoDB.Bson;

using Patrons.CheckIn.API.Controllers;
using Patrons.CheckIn.API.Models.MongoDatabase;
using Patrons.CheckIn.API.Database;

namespace Patrons.CheckIn.API.Services
{
    /// <summary>
    /// Response object for successful login attempts.
    /// </summary>
    public class ManagerLoginResponse
    {
        /// <summary>
        /// Session ID for newly created session.
        /// </summary>
        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; }

        /// <summary>
        /// Level of access for the given session.
        /// </summary>
        /// <value></value>
        [JsonPropertyName("accessLevel")]
        public string AccessLevel { get; set; }
    }

    public interface IManagerService
    {
        // Manager actions
        Task<ManagerLoginResponse> Login(ManagerLoginRequest loginRequest, string ipAddress);
        Task<List<ManagerVenueDocument>> GetVenues(string managerId);
        Task<ManagerResponse> GetSelf(string managerId);
        Task UpdatePassword(string managerId, string newPassword);

        // Service actions
        Task<DiningServiceDocument> StartDiningService(string managerId, string venueId, string areaId);
        Task<GamingServiceDocument> StartGamingService(string managerId, string venueId, string areaId);
        Task StopDiningService(string managerId, string venueId, string areaId);
        Task StopGamingService(string managerId, string venueId, string areaId);

        // Patron actions
        Task DeleteDiningPatron(string managerId, string serviceId, string tableId, string checkInId, string patronId);
        Task UpdateDiningPatron(string managerId, string serviceId, string tableId, string checkInId, string patronId, DiningPatronUpdateRequest update);

        // Dining group actions
        Task<string> MoveDiningGroup(string managerId, string serviceId, string tableId, string checkInIdx, string newTableNumber);

        // Dining table actions
        Task<string> MoveDiningTable(string managerId, string serviceId, string tableId, string newTableNumber);
        Task CloseDiningTable(string managerId, string serviceId, string tableId);

        // Gaming patron actions
        Task DeleteGamingPatron(string managerId, string serviceId, string patronId);
        Task UpdateGamingPatron(string managerId, string serviceId, string patronId, GamingPatronUpdateRequest update);
        Task CheckOutGamingPatron(string managerId, string serviceId, string patronId);

    };

    public class ManagerService : IManagerService
    {
        private IPatronsDatabase _database;
        private IPasswordService _password;
        private ISessionService _session;

        public ManagerService(IPatronsDatabase db, IPasswordService pwd, ISessionService session)
        {
            _database = db;
            _password = pwd;
            _session = session;
        }

        /// <summary>
        /// Ensure that a manager can access a venue. Throw an error if not.
        /// </summary>
        /// <param name="managerId">Manager ID</param>
        /// <param name="venueId">Venue ID</param>
        private async Task _EnsureManagerCanAccessVenue(string managerId, string venueId)
        {
            // Check that the manager can access the venue.
            bool managerCanAccess = await _database.ManagerCanAccessVenue(managerId, venueId);

            // If the manager does not have access, throw an error.
            if (!managerCanAccess) throw new NoAccessException();
        }

        /// <summary>
        /// Ensure that a manager can access a service. Throw an error if they cannot.
        /// </summary>
        /// <param name="managerId">Manager ID</param>
        /// <param name="serviceId">Service ID</param>
        private async Task _EnsureManagerCanAccessService(string managerId, string serviceId)
        {
            // Check that the manager can access the service
            bool managercanAccess = await _database.ManagerCanAccessService(managerId, serviceId);

            // If the manager does not have access, throw an error.
            if (!managercanAccess) throw new NoAccessException();
        }

        /// <summary>
        /// Manager performs login, attempting to gain a new session.
        /// </summary>
        /// <param name="loginInfo">Login information</param>
        /// <param name="ipAddress">IP Address of request</param>
        public async Task<ManagerLoginResponse> Login(ManagerLoginRequest loginInfo, string ipAddress)
        {
            // Throw an ArgumentNullException if loginInfo or any of it's fields are null.
            if (loginInfo == null || loginInfo.Username == null || loginInfo.Password == null)
            {
                throw new ArgumentNullException();
            }

            // Throw an ArgumentNullException if the IpAddress is null.
            if (ipAddress == null) throw new ArgumentNullException();

            // Pull manager information from database.
            var manager = await _database.GetManagerByUsername(loginInfo.Username);

            // Default to a failed login attempt.
            bool canLogIn = false;

            if (manager.IsPasswordReset && loginInfo.Password == manager.Password)
            {
                // If the manager should reset their password and they have provided the correct cleartext password,
                // allow them to login -- assuming that the next phase will require them to reset their password.
                canLogIn = true;
            }
            else if (_password.IsPasswordMatch(loginInfo.Password, manager.Salt, manager.Password))
            {
                // If the manager password + salt combination matches the stored password allow them to login
                canLogIn = true;
            }

            // If the login failed, throw an exception.
            if (!canLogIn) throw new BadLoginException();

            // Create a new session.
            var newSession = new SessionDocument
            {
                Id = ObjectId.GenerateNewId().ToString(),
                // Generate a new sessionID.
                SessionId = await _session.GenerateSessionId(),
                ManagerId = manager.Id,
                IPAddress = ipAddress,
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                // If the manager needs to reset their password, limit the access to "RESET" status.
                AccessLevel = manager.IsPasswordReset ? "RESET" : "FULL",
                IsActive = true
            };

            // Save new session. Note: _session.GenerateSessionId() handles retrying sessionID collisions.
            await _database.SaveSession(newSession);

            // Return manager login response information.
            return new ManagerLoginResponse
            {
                SessionId = newSession.SessionId,
                AccessLevel = newSession.AccessLevel
            };
        }

        /// <summary>
        /// Get information about a manager using their manager ID.
        /// </summary>
        /// <param name="managerId">Manager ID</param>
        /// <returns>Manager information</returns>
        public async Task<ManagerResponse> GetSelf(string managerId)
        {
            // Throw an ArgumentNullException if the manager ID is null.
            if (managerId == null) throw new ArgumentNullException();

            // Lookup the manager.
            var manager = await _database.GetManagerById(managerId);

            // Project limited information into a manager response object.
            return new ManagerResponse
            {
                FirstName = manager.FirstName,
                LastName = manager.LastName,
                Email = manager.Email,
                IsPasswordReset = manager.IsPasswordReset
            };
        }

        /// <summary>
        /// Update a manager's password to a new value.
        /// </summary>
        /// <param name="managerId">Manager ID</param>
        /// <param name="newPassword">New password (clear-text)</param>
        /// <returns></returns>
        public async Task UpdatePassword(string managerId, string newPassword)
        {
            // Throw an ArgumentNullException if managerId or newPassword is null.
            if (managerId == null || newPassword == null) throw new ArgumentNullException();

            // Lookup manager to ensure that it exists.
            var manager = await _database.GetManagerById(managerId);

            // Create a new password hash using the original manager salt.
            var newPasswordInfo = _password.CreatePassword(newPassword);

            // Save the new password.
            await _database.ManagerUpdatePassword(manager.Id, newPasswordInfo.HashedPassword, newPasswordInfo.Salt);

            // Deactivate all previous manager sessions (force logout).
            await _database.ManagerDeactivateSessions(manager.Id);
        }

        /// <summary>
        /// Get the list of venues that a manager is able to access.
        /// </summary>
        /// <param name="managerId">Manager ID</param>
        /// <returns>Manager information for venues that the manager can access.</returns>
        public Task<List<ManagerVenueDocument>> GetVenues(string managerId)
        {
            // Throw an ArgumentNullException if the managerId is null.
            if (managerId == null) throw new ArgumentNullException();

            // Pull manager venue information.
            return _database.GetManagerVenues(managerId);
        }

        /// <summary>
        /// Delete a patron from a dining service.
        /// </summary>
        /// <param name="managerId">Manager ID</param>
        /// <param name="serviceId">Target service ID</param>
        /// <param name="tableId">Target table ID</param>
        /// <param name="checkInId">Target check-in ID</param>
        /// <param name="patronId">Target patron ID</param>
        public async Task DeleteDiningPatron(
                string managerId,
                string serviceId,
                string tableId,
                string checkInId,
                string patronId)
        {
            // Throw an ArgumentNullException if any supplied parameters is null.
            if (managerId == null || serviceId == null || tableId == null || checkInId == null || patronId == null)
            {
                throw new ArgumentNullException();
            }

            // Ensure manager has access to the given venue.
            await _EnsureManagerCanAccessService(managerId, serviceId);

            // Attempt to delete the patron from the venue.
            await _database.DeleteDiningPatron(serviceId, tableId, checkInId, patronId);
        }

        /// <summary>
        /// Update a dining patron's information with new values.
        /// </summary>
        /// <param name="managerId">Manager ID</param>
        /// <param name="serviceId">Target service ID</param>
        /// <param name="tableId">Target table ID</param>
        /// <param name="checkInId">Target check-in ID</param>
        /// <param name="patronId">Target patron ID</param>
        /// <param name="update">New patron information</param>
        public async Task UpdateDiningPatron(
                string managerId,
                string serviceId,
                string tableId,
                string checkInId,
                string patronId,
                DiningPatronUpdateRequest update)
        {
            // If any argument is null throw an ArgumentNullException.
            if (managerId == null || serviceId == null || tableId == null || checkInId == null || patronId == null || update == null)
            {
                throw new ArgumentNullException();
            }

            // Ensure the manager has access to the given service.
            await _EnsureManagerCanAccessService(managerId, serviceId);

            // Pull the dining service information by service ID.
            DiningServiceDocument diningService = await _database.GetDiningServiceById(serviceId);

            // Check that the service is active
            if (!diningService.IsActive) throw new ServiceIsNotActiveException();

            // Locate the specified table.
            var table = diningService.Sittings.Find(sitting => sitting.Id.Equals(tableId));
            // Throw an error if the table doesn't exist.
            if (table == null) throw new TableNotFoundException();

            // Locate the specified check-in.
            var checkIn = table.CheckIns.Find(ci => ci.Id.Equals(checkInId));
            // Throw an error if the check-in doesn't exist.
            if (checkIn == null) throw new CheckInNotFoundExcption();

            // Locate the specified patron.
            // Throw an error if the patron doesn't exist.
            var patron = checkIn.People.Find(p => p.Id.Equals(patronId));
            if (patron == null) throw new PatronNotFoundException();

            // Update the patron object with new information.
            patron.FirstName = update.FirstName;
            patron.PhoneNumber = update.PhoneNumber;

            // Persist change to the database.
            await _database.UpdateDiningPatron(serviceId, tableId, checkInId, patronId, patron);
        }

        /// <summary>
        /// Move a dining group from one table to another.
        /// </summary>
        /// <param name="managerId">Manager ID</param>
        /// <param name="serviceId">Target service ID</param>
        /// <param name="tableId">Target table ID</param>
        /// <param name="checkInId">Target check-in ID</param>
        /// <param name="newTableNumber">New table number</param>
        // TODO Do some analysis of whether this needs to throw NotFound exceptions or if that's for the DB.
        public async Task<string> MoveDiningGroup(
            string managerId,
            string serviceId,
            string tableId,
            string checkInId,
            string newTableNumber)
        {
            // If any paramters are null, throw an ArgumentNullException.
            if (managerId == null
                || serviceId == null
                || tableId == null
                || checkInId == null
                || newTableNumber == null)
            {
                throw new ArgumentNullException();
            }

            // Ensure the manager has access to the given service.
            await _EnsureManagerCanAccessService(managerId, serviceId);

            // Lookup service information.
            var service = await _database.GetDiningServiceById(serviceId);

            // Throw an exception if the service isn't active.
            if (!service.IsActive) throw new ServiceIsNotActiveException();

            // Move the dining group.
            return await _database.MoveDiningGroup(serviceId, tableId, checkInId, newTableNumber);
        }

        /// <summary>
        /// Move a dining table from one table to another, combining tables if neccessary.
        /// </summary>
        /// <param name="managerId">Manager ID</param>
        /// <param name="serviceId">Target service ID</param>
        /// <param name="tableId">Target table ID</param>
        /// <param name="newTableNumber">New table number</param>
        /// <returns>A task that resolves to the new table ID upon completion.</returns>
        public async Task<string> MoveDiningTable(string managerId, string serviceId, string tableId, string newTableNumber)
        {
            // Throw an ArgumentNullException if any parameters are null.
            if (managerId == null || serviceId == null || tableId == null || newTableNumber == null)
            {
                throw new ArgumentNullException();
            }

            // Ensure the manager has access to the given service.
            await _EnsureManagerCanAccessService(managerId, serviceId);

            // Lookup service information.
            var service = await _database.GetDiningServiceById(serviceId);

            // Throw an exception if the service is not active.
            if (!service.IsActive) throw new ServiceIsNotActiveException();

            // Move the table to the new table number, combining tables if necessary.
            return await _database.MoveDiningTable(serviceId, tableId, newTableNumber);
        }

        /// <summary>
        /// Close a dining table, marking it as no longer active.
        /// </summary>
        /// <param name="managerId">Manager ID</param>
        /// <param name="serviceId">Target service ID</param>
        /// <param name="tableId">Target table ID</param>
        public async Task CloseDiningTable(string managerId, string serviceId, string tableId)
        {
            // Ensure that the manager has access to the given service.
            await _EnsureManagerCanAccessService(managerId, serviceId);

            // Lookup service information.
            var service = await _database.GetDiningServiceById(serviceId);

            // Throw an exception if the service is not active.
            if (!service.IsActive) throw new ServiceIsNotActiveException();

            // Close the dining table.
            await _database.CloseDiningTable(serviceId, tableId);
        }

        /// <summary>
        /// Delete a gaming patron from a service. This is a destructive operation.
        /// </summary>
        /// <param name="managerId">Manager ID</param>
        /// <param name="serviceId">Target service ID</param>
        /// <param name="patronId">Target patron ID</param>
        public async Task DeleteGamingPatron(string managerId, string serviceId, string patronId)
        {
            // Ensure the manager has access to the given service.
            await _EnsureManagerCanAccessService(managerId, serviceId);

            // Lookup service information.
            var service = await _database.GetGamingServiceById(serviceId);

            // Throw an exception if the service is not active.
            if (!service.IsActive) throw new ServiceIsNotActiveException();

            // Delete the gaming patron.
            await _database.DeleteGamingPatron(serviceId, patronId);
        }

        /// <summary>
        /// Update information for a gaming patron in a given service.
        /// </summary>
        /// <param name="managerId">Manager ID</param>
        /// <param name="serviceId">Target service ID</param>
        /// <param name="patronId">Target patron ID</param>
        /// <param name="update">New gaming patron information</param>
        public async Task UpdateGamingPatron(string managerId, string serviceId, string patronId, GamingPatronUpdateRequest update)
        {
            // Ensure the manager has access to the given service.
            await _EnsureManagerCanAccessService(managerId, serviceId);

            // Lookup the service.
            var service = await _database.GetGamingServiceById(serviceId);

            // Throw an exception if the service is not active.
            if (!service.IsActive) throw new ServiceIsNotActiveException();

            // Update the gaming patron.
            await _database.UpdateGamingPatron(managerId, patronId, update);
        }

        /// <summary>
        /// Checkout a gaming patron from an active gaming service.
        /// </summary>
        /// <param name="managerId">Manager ID</param>
        /// <param name="serviceId">Target service ID</param>
        /// <param name="patronId">Target patron ID</param>
        public async Task CheckOutGamingPatron(string managerId, string serviceId, string patronId)
        {
            // Ensure the manager has access to the given service.
            await _EnsureManagerCanAccessService(managerId, serviceId);

            // Lookup service information.
            var service = await _database.GetGamingServiceById(serviceId);

            // Throw an exception if the service is not active.
            if (!service.IsActive) throw new ServiceIsNotActiveException();

            // Checkout the gaming patron.
            await _database.CheckOutGamingPatron(serviceId, patronId);
        }

        /// <summary>
        /// Start a new dining service in a specified venue area, if one does not already exist.
        /// If the area already has an active service, this should fail.
        ///
        /// Assumes that the database object will throw the error that an active service exists.
        /// </summary>
        /// <param name="managerId">Manager ID</param>
        /// <param name="venueId">Target venue ID</param>
        /// <param name="areaId">Target area ID</param>
        /// <returns>The created dining service document</returns>
        public async Task<DiningServiceDocument> StartDiningService(string managerId, string venueId, string areaId)
        {
            // Ensure the manager has access to the given service.
            await _EnsureManagerCanAccessVenue(managerId, venueId);

            // Start a new dining service and return it.
            return await _database.StartDiningService(venueId, areaId);
        }

        /// <summary>
        /// Start a new gaming service in a specified venue area, if one does not already exist.
        /// If the area already has an active service, this should fail.
        ///
        /// Assumes that the database object will throw the error that an active service exists.
        /// </summary>
        /// <param name="managerId">Manager ID</param>
        /// <param name="venueId">Target venue ID</param>
        /// <param name="areaId">Target area ID</param>
        /// <returns>The created gaming service document</returns>
        public async Task<GamingServiceDocument> StartGamingService(string managerId, string venueId, string areaId)
        {
            // Ensure the manager has access to the given service.
            await _EnsureManagerCanAccessVenue(managerId, venueId);

            // Start a new gaming service and return it.
            return await _database.StartGamingService(venueId, areaId);
        }

        /// <summary>
        /// Stop the dining service in a given venue area. Throw an error if the service is not currently active.
        /// </summary>
        /// <param name="managerId"></param>
        /// <param name="venueId"></param>
        /// <param name="areaId"></param>
        /// <returns></returns>
        public async Task StopDiningService(string managerId, string venueId, string areaId)
        {
            // Ensure the manager has access to the given service.
            await _EnsureManagerCanAccessVenue(managerId, venueId);

            // Stop the dining service
            await _database.StopDiningService(venueId, areaId);
        }

        /// <summary>
        /// Stop the gaming service in a given venue area. Throw an error if the service is not currently active.
        /// </summary>
        /// <param name="managerId">Manager ID</param>
        /// <param name="venueId">Target venue ID</param>
        /// <param name="areaId">Target area ID</param>
        /// <returns></returns>
        public async Task StopGamingService(string managerId, string venueId, string areaId)
        {
            // Ensure the manager has access to the given service.
            await _EnsureManagerCanAccessVenue(managerId, venueId);

            // Stop the gaming service.
            await _database.StopGamingService(venueId, areaId);
        }
    }
}
