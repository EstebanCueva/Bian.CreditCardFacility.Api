using Bian.CreditCardFacility.Api.Models;

namespace Bian.CreditCardFacility.Api;

public static class Data
{
    private static readonly Dictionary<string, RetrieveCreditCardFacilitiesByCustomer> _db =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["CUST-123"] = new RetrieveCreditCardFacilitiesByCustomer
            {
                CreditCardFacilities = new List<CreditCardFacilityResponse>
                {
                    BuildCard(
                        issuedDeviceId: "ABC12345678",
                        maskedPan: "4562 **** **** 2365",
                        networkId: "VS012",
                        networkName: "Visa",
                        role: CardRoleTypeValues.Primary,
                        usedAmount: 125.50m,
                        minPay: 25.00m,
                        dueDate: "2026-01-05",
                        sessionId: "00000000-0000-0000-0000-000000000001"
                    ),
                    BuildCard(
                        issuedDeviceId: "XYZ98765432",
                        maskedPan: "4111 **** **** 1111",
                        networkId: "MC001",
                        networkName: "Mastercard",
                        role: CardRoleTypeValues.Additional,
                        usedAmount: 980.00m,
                        minPay: 80.00m,
                        dueDate: "2026-01-05",
                        sessionId: "00000000-0000-0000-0000-000000000001"
                    )
                }
            },

            ["CUST-1234"] = new RetrieveCreditCardFacilitiesByCustomer
            {
                CreditCardFacilities = new List<CreditCardFacilityResponse>
                {
                    BuildCard(
                        issuedDeviceId: "QWE11122233",
                        maskedPan: "5100 **** **** 9921",
                        networkId: "MC001",
                        networkName: "Mastercard",
                        role: CardRoleTypeValues.Primary,
                        usedAmount: 35.00m,
                        minPay: 10.00m,
                        dueDate: "2026-01-12",
                        sessionId: "00000000-0000-0000-0000-000000000999"
                    )
                }
            }
        };

    public static bool TryGetByCustomerId(string customerId, out RetrieveCreditCardFacilitiesByCustomer result)
        => _db.TryGetValue(customerId, out result!);

    private static CreditCardFacilityResponse BuildCard(
        string issuedDeviceId,
        string maskedPan,
        string networkId,
        string networkName,
        CardRoleTypeValues role,
        decimal usedAmount,
        decimal minPay,
        string dueDate,
        string sessionId)
    {
        return new CreditCardFacilityResponse
        {
            IssuedDevice = new IssuedDevice
            {
                IssuedDeviceId = issuedDeviceId,
                DevicePropertySetting = maskedPan,
                CardNetwork = new CardNetwork
                {
                    CardNetworkId = networkId,
                    NetworkName = networkName
                },
                CardRole = role
            },
            StatementSchedule = new Schedule
            {
                ScheduleType = new Text { Data = "Monthly" } 
            },
            BillingTransactionAmount = Money(usedAmount, "USD", AmountTypeValues.Used),
            BillingTransactionMinimumRequiredPayment = Money(minPay, "USD", AmountTypeValues.Minimum),
            BillingTransactionPaymentDueDate = dueDate,
            ProductInstanceReference = new CardPaymentAgreement
            {
                CardAmount = Money(usedAmount, "USD", AmountTypeValues.Used)
            },
            CustomerInteraction = new InteractionSession
            {
                Idsession = sessionId
            }
        };
    }

    private static Amount Money(decimal value, string currency, AmountTypeValues type)
    {
        return new Amount
        {
            AmountValue = new Value { Data = value.ToString("0.00") }, 
            AmountCurrency = new CurrencyCode { Currencycode = currency },
            DecimalPointPosition = new Text { Data = "2" }, 
            AmountType = type
        };
    }
}
