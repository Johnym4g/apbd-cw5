using System;

namespace LegacyRenewalApp
{
    public class PaymentFeeCalculator
    {
        public (decimal Fee, string Note) Calculate(string paymentMethod, decimal subtotal)
        {
            return paymentMethod switch
            {
                "CARD" => (subtotal * 0.02m, "card payment fee; "),
                "BANK_TRANSFER" => (subtotal * 0.01m, "bank transfer fee; "),
                "PAYPAL" => (subtotal * 0.035m, "paypal fee; "),
                "INVOICE" => (0m, "invoice payment; "),
                _ => throw new ArgumentException("Unsupported payment method")
            };
        }
    }
}
