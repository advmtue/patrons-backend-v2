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
    }
}