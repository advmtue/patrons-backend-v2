namespace patrons_web_api.Models.Transfer.Response
{
    public class APIError
    {
        public string Message { get; }
        public string Code { get; }

        public APIError(string message, string code)
        {
            this.Message = message;
            this.Code = code;
        }

        public static APIError ManagerNotFound()
        {
            return new APIError("Manager not found", EManagerNotFound);
        }

        public static APIError VenueNotFound()
        {
            return new APIError("Venue not found", EVenueNotFound);
        }

        public static APIError UnknownError()
        {
            return new APIError("Caught unexpected internal server error", EUnknownInternal);
        }

        public static APIError ZeroPatronCount()
        {
            return new APIError("At least one patron must be checked in", EZeroPatrons);
        }

        public static APIError BadLogin()
        {
            return new APIError("Bad login", EBadLogin);
        }

        public static APIError NoAccess()
        {
            return new APIError("You are not authorized to perform actions against the requested resource", ENoAccess);
        }

        public static APIError AreaNotFound()
        {
            return new APIError("Requested venue area was not found", EAreaNotFound);
        }

        public static APIError AreaHasActiveService()
        {
            return new APIError("Area already has an active service", EAreaHasActiveService);
        }

        public static APIError AreaHasNoActiveService()
        {
            return new APIError("Area does not have an active service", EAreaHasNoActiveService);
        }

        public static APIError MarketingUserAlreadySubscribed()
        {
            return new APIError("A matching email address is already subscribed for marketing", EMarketingUserAlreadySubscribed);
        }

        public static APIError PatronNotFound()
        {
            return new APIError("No matching patron was found", EPatronNotFound);
        }

        public static APIError TableNotFound()
        {
            return new APIError("No matching table was found", ETableNotFound);
        }

        public static APIError CheckInNotFound()
        {
            return new APIError("No matching check-in was found", ECheckInNotFound);
        }

        public static APIError RecaptchaFailure()
        {
            return new APIError("Request did not pass recaptcha confidence threshold", ERecaptchaFail);
        }

        public static APIError AreaHasNoService()
        {
            return new APIError("Area has no active service", ENoService);
        }

        // Error code string constants
        const string EAreaHasNoActiveService = "E_AREA_HAS_NO_ACTIVE_SERVICE";
        const string EAreaHasActiveService = "E_AREA_HAS_ACTIVE_SERVICE";
        const string EVenueNotFound = "E_VENUE_NOT_FOUND";
        const string EUnknownInternal = "E_UNKNOWN_INTERNAL";
        const string EZeroPatrons = "E_ZERO_PATRON_COUNT";
        const string EBadLogin = "E_BAD_LOGIN";
        const string ENoAccess = "E_NO_ACCESS";
        const string EAreaNotFound = "E_AREA_NOT_FOUND";
        const string EMarketingUserAlreadySubscribed = "E_MARKETING_USER_SUBSCRIBED";
        const string EManagerNotFound = "E_MANAGER_NOT_FOUND";
        const string EPatronNotFound = "E_PATRON_NOT_FOUND";
        const string ETableNotFound = "E_TABLE_NOT_FOUND";
        const string ECheckInNotFound = "E_CHECKIN_NOT_FOUND";
        const string ERecaptchaFail = "E_RECAPTCHA_FAIL";
        const string ENoService = "E_NO_SERVICE";
    }
}
