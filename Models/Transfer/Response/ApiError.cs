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
            return new APIError("An unknown error occurred", EUnknownInternal);
        }

        public static APIError ZeroPatronCount()
        {
            return new APIError("At least one patron must be checked in", EZeroPatrons);
        }

        // Error code string constants
        const string EVenueNotFound = "E_VENUE_NOT_FOUND";
        const string EUnknownInternal = "E_UNKNOWN_INTERNAL";
        const string EZeroPatrons = "E_ZERO_PATRON_COUNT";
    }
}