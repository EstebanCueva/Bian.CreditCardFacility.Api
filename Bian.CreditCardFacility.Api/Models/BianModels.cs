using System.Text.Json.Serialization;

namespace Bian.CreditCardFacility.Api.Models;

public sealed class RetrieveCreditCardFacilitiesByCustomer
{
    public List<CreditCardFacilityResponse> CreditCardFacilities { get; set; } = new();
}

public sealed class CreditCardFacilityResponse
{
    public IssuedDevice? IssuedDevice { get; set; }
    public Schedule? StatementSchedule { get; set; }
    public Amount? BillingTransactionAmount { get; set; }
    public Amount? BillingTransactionMinimumRequiredPayment { get; set; }

    public string? BillingTransactionPaymentDueDate { get; set; }

    public CardPaymentAgreement? ProductInstanceReference { get; set; }
    public InteractionSession? CustomerInteraction { get; set; }
}

public sealed class InteractionSession
{
    public string? Idsession { get; set; }
}

public sealed class IssuedDevice
{
    public string? IssuedDeviceId { get; set; }
    public string? DevicePropertySetting { get; set; }
    public CardNetwork? CardNetwork { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CardRoleTypeValues? CardRole { get; set; }
}

public sealed class CardNetwork
{
    [JsonPropertyName("cardNetworkid")]
    public string? CardNetworkId { get; set; }

    [JsonPropertyName("cardNetwork")]
    public string? NetworkName { get; set; }
}

public enum CardRoleTypeValues
{
    Primary,
    Additional
}

public sealed class Schedule
{
    public Text? ScheduleType { get; set; }
}

public sealed class Text
{
    [JsonPropertyName("Text")]
    public string? Data { get; set; }
}

public sealed class Amount
{
    public Value? AmountValue { get; set; }
    public CurrencyCode? AmountCurrency { get; set; }
    public Text? DecimalPointPosition { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AmountTypeValues? AmountType { get; set; }
}

public sealed class Value
{
    [JsonPropertyName("Value")]
    public string? Data { get; set; }
}

public sealed class CurrencyCode
{
    [JsonPropertyName("Currencycode")]
    public string? Currencycode { get; set; }
}

public enum AmountTypeValues
{
    Principal,
    Actual,
    Estimated,
    Maximum,
    Default,
    Replacement,
    Incremental,
    Decremental,
    Reserved,
    Available,
    Used,
    DuePayable,
    Minimum,
    Open,
    Unknown,
    Fixed
}

public sealed class CardPaymentAgreement
{
    public Amount? CardAmount { get; set; }
}

public sealed class ErrorResponse
{
    public string Code { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string Message { get; set; } = default!;
    public List<string>? Details { get; set; }
}
