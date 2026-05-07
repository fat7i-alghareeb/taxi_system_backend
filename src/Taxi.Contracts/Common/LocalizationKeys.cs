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
        public const string InvalidOtp = "Auth.InvalidOtp";
        public const string OtpExpired = "Auth.OtpExpired";
        public const string PhoneBlocked = "Auth.PhoneBlocked";
        public const string SessionNotFound = "Auth.SessionNotFound";
        public const string UserIdClaimInvalid = "Auth.UserIdClaim.Invalid";
        public const string RefreshTokenExpired = "Auth.RefreshToken.Expired";
        public const string UserNotFound = "Auth.User.NotFound";
        public const string TokenGenerationFailed = "Auth.TokenGeneration.Failed";
    }

    public static class User
    {
        public const string NameEnRequired = "User.NameEnRequired";
        public const string NameArRequired = "User.NameArRequired";
        public const string NameNlRequired = "User.NameNlRequired";
        public const string PhoneRequired = "User.PhoneRequired";
        public const string Inactive = "User.Inactive";
        public const string NotFound = "User.NotFound";
        public const string NotADriver = "User.NotADriver";
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
        public const string NotFound = "Vehicle.NotFound";
        public const string TypeIdRequired = "Vehicle.TypeId.Required";
        public const string MakeRequired = "Vehicle.Make.Required";
        public const string ModelRequired = "Vehicle.Model.Required";
        public const string LicensePlateRequired = "Vehicle.LicensePlate.Required";
        public const string DriverNotFound = "Vehicle.DriverId.Invalid";
    }

    public static class Driver
    {
        public const string UserIdRequired = "Driver.UserId.Required";
        public const string LicenseRequired = "Driver.License.Required";
        public const string NotFound = "Driver.NotFound";
    }

    public static class Payment
    {
        public const string InvalidAmount = "Payment.Amount.Invalid";
        public const string NotFound = "Payment.NotFound";
    }

    public static class Trip
    {
        public const string QuoteExpired = "Trip.Quote.Expired";
        public const string QuoteNotFound = "Trip.Quote.NotFound";
        public const string InvalidStops = "Trip.Stops.Invalid";
        public const string InvalidStatus = "Trip.Status.Invalid";
        public const string NotFound = "Trip.NotFound";
        public const string PassengerNotFound = "Trip.PassengerId.Invalid";
        public const string VehicleTypeNotFound = "Trip.VehicleType.NotFound";
    }

    public static class Audit
    {
        public const string NotFound = "Audit.NotFound";
    }

    public static class Promo
    {
        public const string CodeRequired = "Promo.Code.Required";
        public const string NotFound = "Promo.NotFound";
    }
}
