using System;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

using patrons_web_api.Services;
using patrons_web_api.Database;
using patrons_web_api.Models.Transfer.Response;

/// <summary>
/// Controller and Objects for handling all management requests.
///
/// Examples of such requests:
///     * Starting/Stopping dining and gaming services
///     * Reviewing patrons, guests, checkIns, sittings, etc in dining and gaming services
///     * Updating patron information in services
///     * Reviewing/closing dining sittings
///     * Reviewing/checking-out gaming patrons
/// </summary>
namespace patrons_web_api.Controllers
{
    /// <summary>
    /// Response object returned when a manager successfully logs-in.
    /// </summary>
    public class ManagerResponse
    {
        /// <summary>
        /// Manager First Name
        /// </summary>
        [JsonPropertyName("firstName")]
        public string FirstName { get; set; }

        /// <summary>
        /// Manager last name
        /// </summary>
        [JsonPropertyName("lastName")]
        public string LastName { get; set; }

        /// <summary>
        /// Manager's email address
        /// </summary>
        [JsonPropertyName("email")]
        public string Email { get; set; }

        /// <summary>
        /// Is the manager currently required to reset their password?
        /// </summary>
        [JsonPropertyName("isPasswordReset")]
        public bool IsPasswordReset { get; set; }
    }

    /// <summary>
    /// Represents the incoming request for updating a dining patron's information.
    /// Does not need to use the patron.id field since it is used in the request.
    /// </summary>
    public class DiningPatronUpdateRequest
    {
        /// <summary>
        /// The patron's first name.
        /// </summary>
        [JsonPropertyName("firstName")]
        public string FirstName { get; set; }

        /// <summary>
        /// The patron's phone number.
        /// </summary>
        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; }
    }

    /// <summary>
    /// Represents the incoming request for updating a gaming patron's information.
    /// </summary>
    public class GamingPatronUpdateRequest
    {
        /// <summary>
        /// Old or updated first name of the gaming patron.
        /// </summary>
        [JsonPropertyName("firstName")]
        public string FirstName { get; set; }

        /// <summary>
        /// Old or updated gaming patron's last name.
        /// </summary>
        [JsonPropertyName("lastName")]
        public string LastName { get; set; }

        /// <summary>
        /// Old or updated gaming patron's phone number.
        /// </summary>
        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; }
    }

    /// <summary>
    /// Represents the incoming request for a manager login.
    /// </summary>
    public class ManagerLoginRequest
    {
        /// <summary>
        /// The manager's username.
        /// </summary>
        [Required]
        [JsonPropertyName("username")]
        public string Username { get; set; }

        /// <summary>
        /// The manager's password.
        /// </summary>
        [Required]
        [JsonPropertyName("password")]
        public string Password { get; set; }
    }

    /// <summary>
    /// Represents the incoming request for a manager password change.
    /// </summary>
    public class UpdatePasswordRequest
    {
        /// <summary>
        /// Manager's requested new password, in plain text.
        /// </summary>
        [JsonPropertyName("newPassword")]
        public string NewPassword { get; set; }
    }

    [Route("manager")]
    [ApiController]
    public class ManagerController : ControllerBase
    {
        private IManagerService _managerService;
        private readonly ILogger _logger;

        public ManagerController(IManagerService managerService, ILogger<ManagerController> logger)
        {
            // Save refs
            _managerService = managerService;
            _logger = logger;
        }

        /// <summary>
        /// Endpoint for manager attempting to login to their account.
        /// </summary>
        /// <param name="login">Encapsulated login information.</param>
        /// <returns>A manager login response, or an error if one occurred.</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] ManagerLoginRequest login)
        {
            try
            {
                // Pull IP address from X-Forwarded-For.
                string ipAddress = HttpContext.Connection.RemoteIpAddress.ToString();

                // Attempt to perform a login, returning a ManagerLoginResponse on success
                return Ok(await _managerService.Login(login, ipAddress));
            }
            catch (BadLoginException)
            {
                // Error: Login credentials are invalid
                return BadRequest(APIError.BadLogin());
            }
            catch (Exception e)
            {
                // Error: Unknown error.
                _logger.LogError(e, "Failed to perform login for {username}", login.Username);

                return BadRequest(APIError.UnknownError());
            }
        }

        /// <summary>
        /// Manager requests their own information, usually called immediately after a successful login.
        /// </summary>
        /// <returns>A manager response.</returns>
        [HttpGet("@self")]
        [Authorize(Policy = "authenticated")]
        public async Task<IActionResult> GetSelf()
        {
            // Pull manager ID from the HTTP context
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                // Use the manager ID to pull manager information.
                return Ok(await _managerService.GetSelf(managerId));
            }
            catch (ManagerNotFoundException e)
            {
                // Error: The manager specified in the request context does not exist.
                // This shouldn't really occur, since the authentication handler should fail before the request gets to the endpoint.
                _logger.LogError(e, "Session managerId resolved to unknown manager. [mId: {managerId}]", managerId);

                return BadRequest(APIError.ManagerNotFound());
            }
            catch (Exception e)
            {
                // Error: Unknown error.
                _logger.LogError(e, "Failed to pull authenticated manager information. [mId: {managerId}]", managerId);

                return BadRequest(APIError.UnknownError());
            }
        }

        /// <summary>
        /// Manager attempts to update their password.
        /// </summary>
        /// <param name="passwordInfo">New password information.</param>
        /// <returns>Empty OK status on success, or an API error on failure.</returns>
        [HttpPost("@updatepassword")]
        [Authorize(Policy = "registrationAccess")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest passwordInfo)
        {
            // Pull manager ID from the HTTP context.
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                // Update the manager's password to the new value.
                await _managerService.UpdatePassword(managerId, passwordInfo.NewPassword);

                // Return an empty OK response, since there doesn't need to be any content.
                return Ok();
            }
            catch (Exception e)
            {
                // Error: Unknown error.
                _logger.LogError(e, "Failed to update manager password. [mId: {managerId}]", managerId);

                return BadRequest(APIError.UnknownError());
            }
        }

        /// <summary>
        /// Manager requests the list of venues which they have management access over.
        /// Response also includes information regarding the active services, and various other information related to the venue.
        /// </summary>
        /// <returns>A list of venues which the manager can access.</returns>
        [HttpGet("@venues")]
        [Authorize(Policy = "fullAccess")]
        public async Task<IActionResult> GetVenues()
        {
            // Pull manager ID from the HTTP context.
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                // Return the list of venues which the manager can access.
                return Ok(new { Venues = await _managerService.GetVenues(managerId) });
            }
            catch (Exception e)
            {
                // Error: Unknown error.
                _logger.LogError(e, "Failed to retrieve manager venues. [mId: {managerId}]", managerId);

                return BadRequest(APIError.UnknownError());
            }
        }

        /// <summary>
        /// Manager requests that a dining service should be stopped in a given venue and area.
        /// </summary>
        /// <param name="venueId">Venue ID of the targetted venue.</param>
        /// <param name="areaId">Area ID of the targetted area, in the targetted venue.</param>
        /// <returns>A new dining service on success, or an error.</returns>
        [HttpGet("dining/venue/{venueId}/area/{areaId}/service/start")]
        [Authorize(Policy = "fullAccess")]
        public async Task<IActionResult> StartDiningService([FromRoute] string venueId, [FromRoute] string areaId)
        {
            // Pull manager ID from the HTTP context.
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                // Start a new service and return it.
                return Ok(new { Service = await _managerService.StartDiningService(managerId, venueId, areaId) });
            }
            catch (NoAccessException e)
            {
                // Error: Manager does not have access to create/start a dining service in the given area.
                _logger.LogInformation(e, "Manager was denied acess to start dining service. [mId: {managerId}, vId: {venueId}, aId: {areaId}]", managerId, venueId, areaId);

                return BadRequest(APIError.NoAccess());
            }
            catch (AreaNotFoundException e)
            {
                // Error: The requested area does not exist.
                _logger.LogError(e, "Cannot start dining service in unknown area. [vId: {venueId}, aId: {areaId}]", venueId, areaId);

                return BadRequest(APIError.AreaNotFound());
            }
            catch (AreaHasActiveServiceException)
            {
                // Error: The area specified already has an active service which needs to be stopped before it can be started again.
                return BadRequest(APIError.AreaHasActiveService());
            }
            catch (Exception e)
            {
                // Error: Unknown error.
                _logger.LogError(e, "Failed to start dining service [vId: {venueId}]", venueId);

                return BadRequest(APIError.UnknownError());
            }
        }

        /// <summary>
        /// Manager requests that a dining service should be stopped in a given venue and area.
        /// </summary>
        /// <param name="venueId">Venue ID of the target venue.</param>
        /// <param name="areaId">Area ID of the target area, in the target venue.</param>
        /// <returns>An empty OK status on success, or an error.</returns>
        [HttpGet("dining/venue/{venueId}/area/{areaId}/service/stop")]
        [Authorize(Policy = "fullAccess")]
        public async Task<IActionResult> StopDiningService([FromRoute] string venueId, [FromRoute] string areaId)
        {
            // Pull manager ID from the HTTP context.
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                // Stop the dining service.
                await _managerService.StopDiningService(managerId, venueId, areaId);

                // Return an empty OK status.
                return Ok();
            }
            catch (NoAccessException e)
            {
                // Error: Manager does not have access to the area/venue which they tried to modify.
                _logger.LogInformation(e, "Manager was denied access to stop dining service. [mId: {managerId}, vId: {venueId}, aId: {areaId}]", managerId, venueId, areaId);

                return BadRequest(APIError.NoAccess());
            }
            catch (AreaNotFoundException e)
            {
                // Error: Area specified doesn't exist.
                _logger.LogError(e, "Cannot stop dining service in unknown area. [vId: {venueId}, aId: {areaId}]", venueId, areaId);

                return BadRequest(APIError.AreaNotFound());
            }
            catch (AreaHasNoActiveServiceException e)
            {
                // Error: Target area does not have an active service, and therefore cannot be stopped.
                _logger.LogError(e, "Cannot stop service in an area that has no active service. [vId: {venueId}, aId: {areaId}]", venueId, areaId);

                return BadRequest(APIError.AreaHasNoActiveService());
            }
            catch (Exception e)
            {
                // Error: Unknown error.
                _logger.LogError(e, "Failed to stop dining service [vId: {venueId}]", venueId);

                return BadRequest(APIError.UnknownError());
            }
        }

        /// <summary>
        /// Manager requests a gaming service be started in a given venue and area.
        /// </summary>
        /// <param name="venueId">Venue ID of target venue.</param>
        /// <param name="areaId">Area ID of target area, in target venue.</param>
        /// <returns>A new gaming service on success, or an error.</returns>
        [HttpGet("gaming/venue/{venueId}/area/{areaId}/service/start")]
        [Authorize(Policy = "fullAccess")]
        public async Task<IActionResult> StartGamingService([FromRoute] string venueId, [FromRoute] string areaId)
        {
            // Pull manager ID from the HTTP context.
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                // Start a new gaming service and return it.
                return Ok(new { Service = await _managerService.StartGamingService(managerId, venueId, areaId) });
            }
            catch (NoAccessException e)
            {
                // Error: Manager does not have access to the specified venue and area.
                _logger.LogInformation(e, "Manager was denied access to start gaming service. [mId: {managerId}, vId: {venueId}, aId: {areaId}]", managerId, venueId, areaId);

                return BadRequest(APIError.NoAccess());
            }
            catch (AreaNotFoundException e)
            {
                // Error: Target area does not exist, or could not be found.
                _logger.LogError(e, "Cannot start gaming service in an unknown area. [vId: {venueId}, aId: {areaId}]", venueId, areaId);

                return BadRequest(APIError.AreaNotFound());
            }
            catch (AreaHasActiveServiceException)
            {
                // Error: Target area already has an active service, and therefore cannot have a new service started.
                return BadRequest(APIError.AreaHasActiveService());
            }
            catch (Exception e)
            {
                // Error: Unknown error.
                _logger.LogError(e, "Failed to start gaming service [vId: {venueId}]", venueId);

                return BadRequest(APIError.UnknownError());
            }
        }

        /// <summary>
        /// Manager requests a gaming service to be stopped in a target venue and area.
        /// </summary>
        /// <param name="venueId">Venue ID of target venue.</param>
        /// <param name="areaId">Area ID of target area.</param>
        /// <returns>An empty OK status on success, or an api error.</returns>
        [HttpGet("gaming/venue/{venueId}/area/{areaId}/service/stop")]
        [Authorize(Policy = "fullAccess")]
        public async Task<IActionResult> StopGamingService([FromRoute] string venueId, [FromRoute] string areaId)
        {
            // Pull manager ID from the HTTP context.
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                // Stop the gaming service in the venue + area.
                await _managerService.StopGamingService(managerId, venueId, areaId);

                // Return an empty OK status.
                return Ok();
            }
            catch (NoAccessException e)
            {
                // Error: Manager does not have access to the target venue/area.
                _logger.LogInformation(e, "Manager was denied access to stop gaming service. [mId: {managerId}, vId: {venueId}, aId: {areaId}]", managerId, venueId, areaId);

                return BadRequest(APIError.NoAccess());
            }
            catch (AreaNotFoundException e)
            {
                // Error: Area does not exist or cannot be found.
                _logger.LogError(e, "Cannot stop gaming service in unknown area. [vId: {venueId}, aId: {areaId}]", venueId, areaId);

                return BadRequest(APIError.AreaNotFound());
            }
            catch (AreaHasNoActiveServiceException e)
            {
                // Error: Area does not have an active service.
                _logger.LogInformation(e, "Gaming service cannot be stoppd as there is no active service in the specified venue/area. [vId: {venueId}, aId: {areaId}]", venueId, areaId);

                return BadRequest(APIError.AreaHasNoActiveService());
            }
            catch (Exception e)
            {
                // Error: Unknown error.
                _logger.LogError(e, "Failed to stop gaming service [vId: {venueId}]", venueId);

                return BadRequest(APIError.UnknownError());
            }
        }

        /// <summary>
        /// Manager requests to delete a patron from a given dining service.
        /// </summary>
        /// <param name="serviceId">Target service ID.</param>
        /// <param name="tableId">Target table ID.</param>
        /// <param name="checkInId">Target check-in ID.</param>
        /// <param name="patronId">Target patron ID.</param>
        /// <returns>An empty OK status on success, or an error.</returns>
        [HttpDelete("dining/service/{serviceId}/table/{tableId}/check-in/{checkInId}/patron/{patronId}")]
        [Authorize(Policy = "fullAccess")]
        public async Task<IActionResult> DeleteDiningPatron(
            [FromRoute] string serviceId,
            [FromRoute] string tableId,
            [FromRoute] string checkInId,
            [FromRoute] string patronId
        )
        {
            // Pull manager ID from the HTTP context.
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                // Delete the patron from the check-in.
                await _managerService.DeleteDiningPatron(managerId, serviceId, tableId, checkInId, patronId);

                // Return an empty OK status.
                return Ok();
            }
            catch (TableNotFoundException e)
            {
                // Error: Table specified could not be found.
                _logger.LogInformation(e, "Could not find specified dining sitting to delete patron. [service: {serviceId}, sitting: {tableId}]", serviceId, tableId);

                return BadRequest(APIError.TableNotFound());
            }
            catch (CheckInNotFoundExcption e)
            {
                // Error: Check-in specified could not be found.
                _logger.LogInformation(e, "Could not find specified dining check-in to delete patron. [service: {serviceId}, sitting: {tableId}, checkIn: {checkInId}]", serviceId, tableId, checkInId);

                return BadRequest(APIError.CheckInNotFound());
            }
            catch (PatronNotFoundException e)
            {
                // Error: Patron specified for deletion does not exist.
                _logger.LogInformation(e,"Could not find specified dining patron for deletion. [mId: {managerId}, sId: {serviceId}, pId: {patronId}]", managerId, serviceId, patronId);

                return BadRequest(APIError.PatronNotFound());
            }
            catch (NoAccessException e)
            {
                // Error: Manager does not have sufficient access to delete this patron.
                _logger.LogInformation(e, "Manager was denied access to delete gaming patron. [mId: {managerId}, sId: {serviceId}]", managerId, serviceId);

                return BadRequest(APIError.NoAccess());
            }
            catch (Exception e)
            {
                // Error: Unknown error.
                _logger.LogError(e, "Failed to delete patron. [sId: {serviceId}]", serviceId);

                return BadRequest(APIError.UnknownError());
            }
        }

        /// <summary>
        /// Manager requests to update a dining patron's information in a given dining service.
        /// </summary>
        /// <param name="serviceId">Target service ID.</param>
        /// <param name="tableId">Target table ID.</param>
        /// <param name="checkInId">Target check-in ID.</param>
        /// <param name="patronId">Target patron ID.</param>
        /// <param name="update">New patron information.</param>
        /// <returns>An empty OK status on success, or an error.</returns>
        [HttpPatch("dining/service/{serviceId}/table/{tableId}/check-in/{checkInId}/patron/{patronId}")]
        [Authorize(Policy = "fullAccess")]
        public async Task<IActionResult> UpdateDiningPatron(
            [FromRoute] string serviceId,
            [FromRoute] string tableId,
            [FromRoute] string checkInId,
            [FromRoute] string patronId,
            [FromBody] DiningPatronUpdateRequest update
        )
        {
            // Pull manager ID from the HTTP context.
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                // Update the dining patron with new information.
                await _managerService.UpdateDiningPatron(managerId, serviceId, tableId, checkInId, patronId, update);

                // Return an empty OK status.
                return Ok();
            }
            catch (TableNotFoundException e)
            {
                // Error: Table specified could not be found.
                _logger.LogInformation(e, "Could not find specified dining sitting to update patron. [service: {serviceId}, sitting: {tableId}]", serviceId, tableId);

                return BadRequest(APIError.TableNotFound());
            }
            catch (CheckInNotFoundExcption e)
            {
                // Error: Check-in specified could not be found.
                _logger.LogInformation(e, "Could not find specified dining check-in to update patron. [service: {serviceId}, sitting: {tableId}, checkIn: {checkInId}]", serviceId, tableId, checkInId);

                return BadRequest(APIError.CheckInNotFound());
            }
            catch (PatronNotFoundException e)
            {
                // Error: Patron specified for update does not exist.
                _logger.LogInformation(e,"Could not find specified dining patron for update. [mId: {managerId}, sId: {serviceId}, pId: {patronId}]", managerId, serviceId, patronId);

                return BadRequest(APIError.PatronNotFound());
            }
            catch (NoAccessException e)
            {
                // Error: Manager does not have sufficient access to update the dining patron.
                _logger.LogInformation(e, "Manager was denied access to update dining patron. [mId: {managerId}, sId: {serviceId}]", managerId, serviceId);

                return BadRequest(APIError.NoAccess());
            }
            catch (Exception e)
            {
                // Error: Unknown error.
                _logger.LogError(e, "Failed to update dining patron");

                return BadRequest(APIError.UnknownError());
            }
        }

        /// <summary>
        /// Manager requests to move a dining group from one table to another.
        /// </summary>
        /// <param name="serviceId">Target service ID.</param>
        /// <param name="tableId">Target table ID.</param>
        /// <param name="checkInId">Target check-in ID.</param>
        /// <param name="tableNumber">New table number.</param>
        /// <returns>The ID of the table which the group has been migrated to.</returns>
        [HttpGet("dining/service/{serviceId}/table/{tableId}/check-in/{checkInId}/move/{tableNumber}")]
        [Authorize(Policy = "fullAccess")]
        public async Task<IActionResult> MoveDiningGroup(
            [FromRoute] string serviceId,
            [FromRoute] string tableId,
            [FromRoute] string checkInId,
            [FromRoute] string tableNumber
        )
        {
            // Pull manager ID from the HTTP context.
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                // Move the dining group and return the table ID.
                return Ok(new { TableId = await _managerService.MoveDiningGroup(managerId, serviceId, tableId, checkInId, tableNumber) });
            }
            catch (TableNotFoundException e)
            {
                // Error: Table specified could not be found.
                _logger.LogInformation(e, "Could not find specified dining sitting. [service: {serviceId}, sitting: {tableId}]", serviceId, tableId);

                return BadRequest(APIError.TableNotFound());
            }
            catch (CheckInNotFoundExcption e)
            {
                // Error: Check-in specified could not be found.
                _logger.LogInformation(e, "Could not find specified dining check-in. [service: {serviceId}, sitting: {tableId}, checkIn: {checkInId}]", serviceId, tableId, checkInId);

                return BadRequest(APIError.CheckInNotFound());
            }
            catch (NoAccessException e)
            {
                // Error: Manager does not have sufficient access to move the dining group.
                _logger.LogInformation(e, "Manager was denied access to move dining group. [mId: {managerId}, sId: {serviceId}]", managerId, serviceId);

                return BadRequest(APIError.NoAccess());
            }
            catch (Exception e)
            {
                // Error: Unknown error.
                _logger.LogError(e, "Failed to move dining check-in");

                return BadRequest(APIError.UnknownError());
            }
        }

        /// <summary>
        /// Manager requests to move a dining table from one table to another.
        /// </summary>
        /// <param name="serviceId">Target service ID.</param>
        /// <param name="tableId">Target table ID.</param>
        /// <param name="tableNumber">New table number.</param>
        /// <returns>The ID of the table which the table has been migrated to.</returns>
        [HttpGet("dining/service/{serviceId}/table/{tableId}/move/{tableNumber}")]
        [Authorize(Policy = "fullAccess")]
        public async Task<IActionResult> MoveDiningTable(
            [FromRoute] string serviceId,
            [FromRoute] string tableId,
            [FromRoute] string tableNumber
        )
        {
            // Pull manager ID from the HTTP context.
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                // Move the dining group and return the table ID where it has been migrated to.
                return Ok(new { TableId = await _managerService.MoveDiningTable(managerId, serviceId, tableId, tableNumber) });
            }
            catch (TableNotFoundException e)
            {
                // Error: Table specified could not be found.
                _logger.LogInformation(e, "Could not find specified dining sitting. [service: {serviceId}, sitting: {tableId}]", serviceId, tableId);

                return BadRequest(APIError.TableNotFound());
            }
            catch (NoAccessException e)
            {
                // Error: Manager does not have sufficient access to move the dining table.
                _logger.LogInformation(e, "Manager was denied access to move dining table. [mId: {managerId}, sId: {serviceId}]", managerId, serviceId);

                return BadRequest(APIError.NoAccess());
            }
            catch (Exception e)
            {
                // Error: Unknown error.
                _logger.LogError(e, "Failed to move dining table");

                return BadRequest(APIError.UnknownError());
            }
        }

        /// <summary>
        /// Manager requests to close a dining table.
        /// </summary>
        /// <param name="serviceId">Target service ID.</param>
        /// <param name="tableId">Target table ID.</param>
        /// <returns>An empty OK status on success, or an error.</returns>
        [HttpGet("dining/service/{serviceId}/table/{tableId}/close")]
        [Authorize(Policy = "fullAccess")]
        public async Task<IActionResult> CloseDiningTable([FromRoute] string serviceId, [FromRoute] string tableId)
        {
            // Pull manager ID from the HTTP context
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                // Close the dining table.
                await _managerService.CloseDiningTable(managerId, serviceId, tableId);

                // Return an empty OK status.
                return Ok();
            }
            catch (TableNotFoundException e)
            {
                // Error: Table specified could not be found.
                _logger.LogInformation(e, "Could not find specified dining sitting. [service: {serviceId}, sitting: {tableId}]", serviceId, tableId);

                return BadRequest(APIError.TableNotFound());
            }
            catch (NoAccessException e)
            {
                // Error: Manager does not have sufficient permission to close the requested dining table.
                _logger.LogInformation(e, "Manager was denied access to close dining table. [mId: {managerId}, sId: {serviceId}]", managerId, serviceId);

                return BadRequest(APIError.NoAccess());
            }
            catch (Exception e)
            {
                // Error: Unknown error.
                _logger.LogError(e, "Failed to close dining table");

                return BadRequest(APIError.UnknownError());
            }
        }

        /// <summary>
        /// Manager requests to delete a patron from a gaming service.
        /// </summary>
        /// <param name="serviceId">Target gaming service ID.</param>
        /// <param name="patronId">Target patron ID.</param>
        /// <returns>Empty OK status on success, or an error.</returns>
        [HttpDelete("gaming/service/{serviceId}/patron/{patronId}")]
        [Authorize(Policy = "fullAccess")]
        public async Task<IActionResult> DeleteGamingPatron([FromRoute] string serviceId, [FromRoute] string patronId)
        {
            // Pull manager ID from the HTTP context.
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                // Delete the gaming patron.
                await _managerService.DeleteGamingPatron(managerId, serviceId, patronId);

                // Return an empty OK status.
                return Ok();
            }
            catch (PatronNotFoundException e)
            {
                // Error: Patron specified for deletion does not exist.
                _logger.LogInformation(e,"Could not find specified gaming patron for deletion. [mId: {managerId}, sId: {serviceId}, pId: {patronId}]", managerId, serviceId, patronId);

                return BadRequest(APIError.PatronNotFound());
            }
            catch (NoAccessException e)
            {
                // Error: Manager does not have sufficient access to delete the gaming patron.
                _logger.LogInformation(e, "Manager was denied access to delete gaming patron. [mId: {managerId}, sId: {serviceId}]", managerId, serviceId);

                return BadRequest(APIError.NoAccess());
            }
            catch (Exception e)
            {
                // Error: Unknown error.
                _logger.LogError(e, "Failed to delete gaming patron");

                return BadRequest(APIError.UnknownError());
            }
        }

        /// <summary>
        /// Manager requests to update a gaming patron with new information.
        /// </summary>
        /// <param name="serviceId">Target service ID.</param>
        /// <param name="patronId">Target patron ID.</param>
        /// <param name="update">New patron information.</param>
        /// <returns>An empty OK status on success, or an error.</returns>
        [HttpPatch("gaming/service/{serviceId}/patron/{patronId}")]
        [Authorize(Policy = "fullAccess")]
        public async Task<IActionResult> UpdateGamingPatron(
            [FromRoute] string serviceId,
            [FromRoute] string patronId,
            [FromBody] GamingPatronUpdateRequest update
        )
        {
            // Pull manager ID from the HTTP context.
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                // Update the gaming patron with new information.
                await _managerService.UpdateGamingPatron(managerId, serviceId, patronId, update);

                // Return an empty OK status.
                return Ok();
            }
            catch (PatronNotFoundException e)
            {
                // Error: Patron specified not found.
                _logger.LogInformation(e,"Could not find specified gaming patron for update. [mId: {managerId}, sId: {serviceId}, pId: {patronId}]", managerId, serviceId, patronId);

                return BadRequest(APIError.PatronNotFound());
            }
            catch (NoAccessException e)
            {
                // Error: Manager does not have sufficient permission to update the gaming patron.
                _logger.LogInformation(e, "Manager was denied access to update gaming patron. [mId: {managerId}, sId: {serviceId}]", managerId, serviceId);

                return BadRequest(APIError.NoAccess());
            }
            catch (Exception e)
            {
                // Error: Unknown error.
                _logger.LogError(e, "Failed to update gaming patron");

                return BadRequest(APIError.UnknownError());
            }
        }

        /// <summary>
        /// Manager requests to checkout a gaming patron from a given gaming service.
        /// </summary>
        /// <param name="serviceId">Target gaming service ID.</param>
        /// <param name="patronId">Target patron ID.</param>
        /// <returns>An empty OK status on success, or an error.</returns>
        [HttpGet("gaming/service/{serviceId}/patron/{patronId}/checkout")]
        [Authorize(Policy = "fullAccess")]
        public async Task<IActionResult> CheckOutGamingPatron([FromRoute] string serviceId, [FromRoute] string patronId)
        {
            // Pull manager ID from the HTTP context.
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                // Checkout the gaming patron.
                await _managerService.CheckOutGamingPatron(managerId, serviceId, patronId);

                // Return an empty OK status.
                return Ok();
            }
            catch (PatronNotFoundException e)
            {
                // Error: Patron specified not found.
                _logger.LogInformation(e,"Could not find specified gaming patron for update. [mId: {managerId}, sId: {serviceId}, pId: {patronId}]", managerId, serviceId, patronId);

                return BadRequest(APIError.PatronNotFound());
            }
            catch (NoAccessException e)
            {
                // Error: Manager does not have sufficient permissions to checkout the gaming patron.
                _logger.LogInformation(e, "Manager was denied access to checkout gaming patron. [mId: {managerId}, sId: {serviceId}]", managerId, serviceId);

                return BadRequest(APIError.NoAccess());
            }
            catch (Exception e)
            {
                // Error: Unknown error.
                _logger.LogError(e, "Failed to checkout gaming patron");

                return BadRequest(APIError.UnknownError());
            }
        }
    }
}
