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
        public const string HomeAddressInvalid = "User.HomeAddressInvalid";
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
        public const string RefundUnavailable = "Payment.Refund.Unavailable";
        public const string RefundStripeDisabled = "Payment.Refund.StripeDisabled";
        public const string RefundFullyRefunded = "Payment.Refund.FullyRefunded";
        public const string RefundExceedsAvailable = "Payment.Refund.ExceedsAvailable";
        public const string RefundDuplicate = "Payment.Refund.Duplicate";
        public const string RefundForcedFailure = "Payment.Refund.ForcedFailure";
        public const string RefundRetryBlocked = "Payment.Refund.RetryBlocked";
        public const string RefundCustomerFailureMessage = "Payment.Refund.CustomerFailureMessage";
        public const string RefundFailedAdminTitle = "Payment.Refund.Failed.AdminTitle";
        public const string RefundFailedAdminBody = "Payment.Refund.Failed.AdminBody";
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
        public const string ScheduledEnRouteNotReady = "Trip.Scheduled.EnRouteNotReady";
        public const string ScheduledArrivalNotReady = "Trip.Scheduled.ArrivalNotReady";
        public const string ScheduledStartNotReady = "Trip.Scheduled.StartNotReady";
        public const string AlreadyAccepted = "Trip.AlreadyAccepted";
        public const string NotAcceptedByCurrentAdmin = "Trip.NotAcceptedByCurrentAdmin";
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
        public const string FlightNumberRequired = "Trip.FlightNumber.Required";
        public const string FlightNumberInvalid = "Trip.FlightNumber.Invalid";
        public const string InvalidRating = "Trip.Rating.Invalid";
        public const string ChatClosed = "Trip.Chat.Closed";
        public const string ChatNotParticipant = "Trip.Chat.NotParticipant";
        public const string ChatEmptyMessage = "Trip.Chat.EmptyMessage";
        public const string ChatMessageTooLong = "Trip.Chat.MessageTooLong";
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

    public static class CustomerIncident
    {
        public const string NotFound = "CustomerIncident.NotFound";
        public const string AlreadyClosed = "CustomerIncident.AlreadyClosed";
        public const string InvalidStatus = "CustomerIncident.InvalidStatus";
        public const string ContactTitleRequired = "CustomerIncident.Contact.TitleRequired";
        public const string ContactBodyRequired = "CustomerIncident.Contact.BodyRequired";
        public const string NoRefundablePayment = "CustomerIncident.NoRefundablePayment";
        public const string RefundFailed = "CustomerIncident.RefundFailed";
    }

    public static class RefundIssue
    {
        public const string NotFound = "RefundIssue.NotFound";
        public const string InvalidRequestType = "RefundIssue.InvalidRequestType";
        public const string InvalidReviewStatus = "RefundIssue.InvalidReviewStatus";
        public const string AlreadyClosed = "RefundIssue.AlreadyClosed";
        public const string CreatedAdminTitle = "RefundIssue.Created.AdminTitle";
        public const string CreatedAdminBody = "RefundIssue.Created.AdminBody";
        public const string RetrySucceededAdminTitle = "Refund.Retry.Succeeded.AdminTitle";
        public const string RetrySucceededAdminBody = "Refund.Retry.Succeeded.AdminBody";
        public const string RetryFailedAdminTitle = "Refund.Retry.Failed.AdminTitle";
        public const string RetryFailedAdminBody = "Refund.Retry.Failed.AdminBody";
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

        // Redesigned PDF layout (mockup).
        public const string VatId = "Invoice.VatId";
        public const string Status = "Invoice.Status";
        public const string StatusPaid = "Invoice.Status.Paid";
        public const string TripDetails = "Invoice.TripDetails";
        public const string Route = "Invoice.Route";
        public const string ServiceType = "Invoice.ServiceType";
        public const string ServiceTitle = "Invoice.ServiceTitle";
        public const string RideDate = "Invoice.RideDate";
        public const string WaitingFee = "Invoice.WaitingFee";
        public const string ColumnTotal = "Invoice.Column.Total";
        public const string Subtotal = "Invoice.Subtotal";
        public const string Vat = "Invoice.Vat";
        public const string Total = "Invoice.Total";
        public const string TotalPaid = "Invoice.TotalPaid";
        public const string PaidVia = "Invoice.PaidVia";
        public const string TransactionId = "Invoice.TransactionId";
        public const string PaymentCompleted = "Invoice.PaymentCompleted";
        public const string MethodIdeal = "Invoice.Method.iDEAL";
        public const string MethodKlarna = "Invoice.Method.Klarna";

        // Issuer company/legal block under the brand name.
        public const string CompanyCountry = "Invoice.Company.Country";
        public const string CompanyKvk = "Invoice.Company.Kvk";
        public const string CompanyBtwId = "Invoice.Company.BtwId";
    }

    public static class AdminProfile
    {
        public const string NameRequired = "AdminProfile.Name.Required";
        public const string EmailRequired = "AdminProfile.Email.Required";
        public const string EmailInvalid = "AdminProfile.Email.Invalid";
        public const string PhoneInvalid = "AdminProfile.Phone.Invalid";
        public const string NotFound = "AdminProfile.NotFound";
    }

    public static class PassengerPaymentMethod
    {
        public const string PassengerIdRequired = "PassengerPaymentMethod.PassengerId.Required";
        public const string GatewayPaymentMethodIdRequired = "PassengerPaymentMethod.GatewayPaymentMethodId.Required";
        public const string CardBrandRequired = "PassengerPaymentMethod.CardBrand.Required";
        public const string LastFourInvalid = "PassengerPaymentMethod.LastFour.Invalid";
        public const string ExpiryMonthInvalid = "PassengerPaymentMethod.ExpiryMonth.Invalid";
        public const string ExpiryYearInvalid = "PassengerPaymentMethod.ExpiryYear.Invalid";
        public const string NotFound = "PassengerPaymentMethod.NotFound";
    }

    public static class Notification
    {
        public const string TitleRequired = "Notification.Title.Required";
        public const string BodyRequired = "Notification.Body.Required";

        public const string DriverArrivedTitle = "Notification.DriverArrived.Title";
        public const string DriverArrivedBody = "Notification.DriverArrived.Body";

        public const string DriverArrivedEarlyTitle = "Notification.DriverArrivedEarly.Title";
        public const string DriverArrivedEarlyBody = "Notification.DriverArrivedEarly.Body";

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
        public const string TripAcceptedTitle = "Notification.TripAccepted.Title";
        public const string TripAcceptedBody = "Notification.TripAccepted.Body";
        public const string AdminScheduledTripTitle = "Notification.AdminScheduledTrip.Title";
        public const string AdminScheduledTripBody = "Notification.AdminScheduledTrip.Body";
        public const string AdminTripReminder60Title = "Notification.AdminTripReminder60.Title";
        public const string AdminTripReminder60Body = "Notification.AdminTripReminder60.Body";
        public const string AdminTripReminder30Title = "Notification.AdminTripReminder30.Title";
        public const string AdminTripReminder30Body = "Notification.AdminTripReminder30.Body";
        public const string AdminTripReminder15Title = "Notification.AdminTripReminder15.Title";
        public const string AdminTripReminder15Body = "Notification.AdminTripReminder15.Body";
        public const string AdminTripOverdueTitle = "Notification.AdminTripOverdue.Title";
        public const string AdminTripOverdueBody = "Notification.AdminTripOverdue.Body";
        public const string AdminAcceptedReminder30Title = "Notification.AdminAcceptedReminder30.Title";
        public const string AdminAcceptedReminder30Body = "Notification.AdminAcceptedReminder30.Body";
        public const string AdminAcceptedReminder15Title = "Notification.AdminAcceptedReminder15.Title";
        public const string AdminAcceptedReminder15Body = "Notification.AdminAcceptedReminder15.Body";

        public const string WaitingFeeDueTitle = "Notification.WaitingFeeDue.Title";
        public const string WaitingFeeDueBody = "Notification.WaitingFeeDue.Body";

        public const string NewMessageTitle = "Notification.NewMessage.Title";
        public const string NewMessageBody = "Notification.NewMessage.Body";
        public const string NewPhotoMessageBody = "Notification.NewMessage.PhotoBody";
    }
}


