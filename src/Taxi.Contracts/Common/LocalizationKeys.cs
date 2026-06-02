namespace Taxi.Contracts.Common;

public static class LocalizationKeys
{
    public static class Car
    {
        public const string IdRequired = "Car.Id.Required";
        public const string MakeRequired = "Car.Make.Required";
        public const string ModelRequired = "Car.Model.Required";
        public const string DescriptionRequired = "Car.Description.Required";
        public const string InvalidYear = "Car.Year.Invalid";
        public const string NotFound = "Car.NotFound";
    }

    public static class RefreshToken
    {
        public const string IdRequired = "RefreshToken.Id.Required";
        public const string TokenRequired = "RefreshToken.Token.Required";
        public const string UserIdRequired = "RefreshToken.UserId.Required";
        public const string ExpiryInvalid = "RefreshToken.Expiry.Invalid";
    }

    public static class Auth
    {
        public const string ExpiredAccessTokenInvalid = "Auth.ExpiredAccessToken.Invalid";
        public const string PhoneBlocked = "Auth.PhoneBlocked";
        public const string SessionNotFound = "Auth.SessionNotFound";
        public const string UserIdClaimInvalid = "Auth.UserIdClaim.Invalid";
        public const string RefreshTokenExpired = "Auth.RefreshToken.Expired";
        public const string UserNotFound = "Auth.User.NotFound";
        public const string TokenGenerationFailed = "Auth.TokenGeneration.Failed";
        public const string InvalidFirebaseToken = "Auth.InvalidFirebaseToken";
        public const string FirebaseTokenExpired = "Auth.FirebaseTokenExpired";
        public const string FirebasePhoneMissing = "Auth.FirebasePhoneMissing";
        public const string PhoneMismatch = "Auth.PhoneMismatch";
        public const string Unauthorized = "Identity.Unauthorized";
    }

    public static class User
    {
        public const string NameRequired = "User.NameRequired";
        public const string NameEnRequired = "User.NameEnRequired";
        public const string NameArRequired = "User.NameArRequired";
        public const string NameNlRequired = "User.NameNlRequired";
        public const string NameDeRequired = "User.NameDeRequired";
        public const string NamePlRequired = "User.NamePlRequired";
        public const string NameUkRequired = "User.NameUkRequired";
        public const string NameFrRequired = "User.NameFrRequired";
        public const string NameEsRequired = "User.NameEsRequired";
        public const string NameRoRequired = "User.NameRoRequired";
        public const string PhoneRequired = "User.PhoneRequired";
        public const string Inactive = "User.Inactive";
        public const string NotFound = "User.NotFound";
        public const string NotADriver = "User.NotADriver";
        public const string ProfileNameRequired = "User.Profile.NameRequired";
        public const string ProfilePhotoInvalid = "User.Profile.PhotoInvalid";
        public const string FcmTokenInvalid = "User.FcmTokenInvalid";
        public const string PreferredLanguageRequired = "User.PreferredLanguageRequired";
        public const string PreferredLanguageInvalid = "User.PreferredLanguageInvalid";
        public const string StripeCustomerIdRequired = "User.StripeCustomerIdRequired";
    }

    public static class Validation
    {
        public const string EnglishNameRequired = "Validation.EnglishName.Required";
        public const string ArabicNameRequired = "Validation.ArabicName.Required";
        public const string EmailInvalid = "Validation.Email.Invalid";
        public const string EmailRequired = "Validation.Email.Required";
        public const string PhoneNumberRequired = "Validation.PhoneNumber.Required";
        public const string PhoneNumberFormat = "Validation.PhoneNumber.Format";
        public const string PasswordRequired = "Validation.Password.Required";
        public const string PasswordMinLength = "Validation.Password.MinLength";
        public const string CustomerIdRequired = "Validation.CustomerId.Required";
        public const string RequiredField = "Validation.RequiredField";
        public const string InvalidFormat = "Validation.InvalidFormat";
        public const string PageInvalid = "Validation.Page.Invalid";
        public const string PageSizeInvalid = "Validation.PageSize.Invalid";
        public const string MakeRequired = "Validation.Make.Required";
        public const string ModelRequired = "Validation.Model.Required";
        public const string YearInvalid = "Validation.Year.Invalid";
        public const string DescriptionRequired = "Validation.Description.Required";
        public const string VehicleTypeIdRequired = "Validation.VehicleTypeId.Required";
    }

    public static class Vehicle
    {
        public const string CodeRequired = "Vehicle.Code.Required";
        public const string NameEnRequired = "Vehicle.NameEn.Required";
        public const string NameArRequired = "Vehicle.NameAr.Required";
        public const string NameNlRequired = "Vehicle.NameNl.Required";
        public const string NameDeRequired = "Vehicle.NameDe.Required";
        public const string NamePlRequired = "Vehicle.NamePl.Required";
        public const string NameUkRequired = "Vehicle.NameUk.Required";
        public const string NameFrRequired = "Vehicle.NameFr.Required";
        public const string NameEsRequired = "Vehicle.NameEs.Required";
        public const string NameRoRequired = "Vehicle.NameRo.Required";
        public const string NotFound = "Vehicle.NotFound";
    }

    public static class Driver
    {
        public const string UserIdRequired = "Driver.UserId.Required";
        public const string LicenseRequired = "Driver.License.Required";
        public const string NotFound = "Driver.NotFound";
        public const string NotApproved = "Driver.NotApproved";
        public const string Inactive = "Driver.Inactive";
        public const string DocumentsNotApproved = "Driver.DocumentsNotApproved";
        public const string NoActiveVehicle = "Driver.NoActiveVehicle";
        public const string InvalidApprovalTransition = "Driver.InvalidApprovalTransition";
    }

    public static class DriverDocument
    {
        public const string DriverIdRequired = "DriverDocument.DriverIdRequired";
        public const string FileUrlRequired = "DriverDocument.FileUrlRequired";
        public const string RejectionNotesRequired = "DriverDocument.RejectionNotesRequired";
        public const string NotFound = "DriverDocument.NotFound";
    }

    public static class Payment
    {
        public const string InvalidAmount = "Payment.Amount.Invalid";
        public const string NotFound = "Payment.NotFound";
        public const string StripeInitiationFailed = "Payment.Stripe.InitiationFailed";
        public const string StripeSignatureInvalid = "Payment.Stripe.SignatureInvalid";
        public const string StripeIntentNotFound = "Payment.Stripe.IntentNotFound";
        public const string WebhookHandlingFailed = "Payment.Stripe.WebhookHandlingFailed";
    }

    public static class Trip
    {
        public const string QuoteExpired = "Trip.Quote.Expired";
        public const string QuoteNotFound = "Trip.Quote.NotFound";
        public const string QuoteAlreadyUsed = "Trip.Quote.AlreadyUsed";
        public const string InvalidStops = "Trip.Stops.Invalid";
        public const string InvalidCoordinate = "Trip.Coordinate.Invalid";
        public const string InvalidStatus = "Trip.Status.Invalid";
        public const string NotFound = "Trip.NotFound";
        public const string PassengerNotFound = "Trip.PassengerId.Invalid";
        public const string VehicleTypeNotFound = "Trip.VehicleType.NotFound";
        public const string DriverNotFound = "Trip.Driver.NotFound";
        public const string ScheduledAtTooSoon = "Trip.ScheduledAt.TooSoon";
        public const string ScheduledNotReady = "Trip.Scheduled.NotReady";
        public const string CannotCancel = "Trip.CannotCancel";
        public const string CancellationWindowExpired = "Trip.Cancellation.WindowExpired";
        public const string DriverCancelTooEarly = "Trip.Cancellation.DriverTooEarly";
        public const string InvalidCancellationReason = "Trip.Cancellation.InvalidReason";
        public const string CompensationClaimNoteRequired = "Trip.CompensationClaim.NoteRequired";
        public const string CompensationClaimAlreadyReviewed = "Trip.CompensationClaim.AlreadyReviewed";
        public const string ActiveWaitingSessionExists = "Trip.Waiting.ActiveSessionExists";
        public const string ActiveWaitingSessionNotFound = "Trip.Waiting.ActiveSessionNotFound";
        public const string NotOwnedByPassenger = "Trip.NotOwnedByPassenger";
        public const string DriverMismatch = "Trip.DriverMismatch";
        public const string StopNotFound = "Trip.Stop.NotFound";
        public const string StopOutOfOrder = "Trip.Stop.OutOfOrder";
        public const string StopAlreadyCompleted = "Trip.Stop.AlreadyCompleted";
        public const string PendingStopsRemaining = "Trip.Stop.PendingStopsRemaining";
        public const string TooManyStops = "Trip.Stops.TooMany";
        public const string PassengerNoteTooLong = "Trip.PassengerNote.TooLong";
        public const string CannotUpdatePassengerNote = "Trip.PassengerNote.CannotUpdate";
    }

    public static class Maps
    {
        public const string QueryRequired = "Maps.Search.QueryRequired";
        public const string LocationRequired = "Maps.Search.LocationRequired";
        public const string CoordinateInvalid = "Maps.Geocode.CoordinateInvalid";
        public const string InsufficientStops = "Maps.Directions.InsufficientStops";
    }

    public static class Audit
    {
        public const string NotFound = "Audit.NotFound";
    }

    public static class AppConfig
    {
        public const string KeyRequired = "AppConfig.Key.Required";
        public const string ValueRequired = "AppConfig.Value.Required";
        public const string NotFound = "AppConfig.NotFound";
    }

    public static class Promo
    {
        public const string CodeRequired = "Promo.Code.Required";
        public const string NotFound = "Promo.NotFound";
    }

    public static class Invoice
    {
        public const string NotIssued = "Invoice.NotIssued";
        public const string NotFound = "Invoice.NotFound";
        public const string AlreadyIssued = "Invoice.AlreadyIssued";
        public const string TripIdRequired = "Invoice.TripIdRequired";
        public const string PassengerIdRequired = "Invoice.PassengerIdRequired";
        public const string NumberRequired = "Invoice.NumberRequired";
        public const string CurrencyRequired = "Invoice.CurrencyRequired";
        public const string InvalidAmount = "Invoice.InvalidAmount";
        public const string InvalidTaxRate = "Invoice.InvalidTaxRate";

        public const string Title = "Invoice.Title";
        public const string Number = "Invoice.Number";
        public const string Date = "Invoice.Date";
        public const string TransactionDate = "Invoice.TransactionDate";
        public const string ColumnDescription = "Invoice.Column.Description";
        public const string ColumnQuantity = "Invoice.Column.Quantity";
        public const string ColumnTaxRate = "Invoice.Column.TaxRate";
        public const string ColumnTaxAmount = "Invoice.Column.TaxAmount";
        public const string ColumnNet = "Invoice.Column.Net";
        public const string TotalNet = "Invoice.TotalNet";
        public const string LineItemTransport = "Invoice.LineItem.Transport";
        public const string PaymentMethod = "Invoice.Payment.Method";
        public const string PaymentCash = "Invoice.Payment.Cash";
        public const string PaymentCard = "Invoice.Payment.Card";
        public const string PaymentWallet = "Invoice.Payment.Wallet";
        public const string BilledTo = "Invoice.BilledTo";
        public const string IssuedBy = "Invoice.IssuedBy";
        public const string VatNumber = "Invoice.VatNumber";
    }

    public static class Notification
    {
        public const string TitleRequired = "Notification.Title.Required";
        public const string BodyRequired = "Notification.Body.Required";

        public const string DriverArrivedTitle = "Notification.DriverArrived.Title";
        public const string DriverArrivedBody = "Notification.DriverArrived.Body";

        public const string DriverEnRouteTitle = "Notification.DriverEnRoute.Title";
        public const string DriverEnRouteBody = "Notification.DriverEnRoute.Body";

        public const string DriverAssignedTitle = "Notification.DriverAssigned.Title";
        public const string DriverAssignedBody = "Notification.DriverAssigned.Body";

        public const string TripStartedTitle = "Notification.TripStarted.Title";
        public const string TripStartedBody = "Notification.TripStarted.Body";

        public const string TripCompletedTitle = "Notification.TripCompleted.Title";
        public const string TripCompletedBody = "Notification.TripCompleted.Body";

        public const string TripCancelledTitle = "Notification.TripCancelled.Title";
        public const string TripCancelledBody = "Notification.TripCancelled.Body";

        public const string TripRequestedTitle = "Notification.TripRequested.Title";
        public const string TripRequestedBody = "Notification.TripRequested.Body";

        public const string TripScheduledConfirmedTitle = "Notification.TripScheduledConfirmed.Title";
        public const string TripScheduledConfirmedBody = "Notification.TripScheduledConfirmed.Body";

        public const string TripScheduledActivatedTitle = "Notification.TripScheduledActivated.Title";
        public const string TripScheduledActivatedBody = "Notification.TripScheduledActivated.Body";
    }
}


