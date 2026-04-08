using System.Collections.Generic;

namespace LegacyRenewalApp
{
    public class DiscountCalculator
    {
        private readonly IEnumerable<IDiscountStrategy> _strategies;

        public DiscountCalculator(IEnumerable<IDiscountStrategy> strategies)
        {
            _strategies = strategies;
        }

        public (decimal TotalDiscount, string Notes) Calculate(Customer customer, SubscriptionPlan plan, decimal baseAmount, int seatCount, bool useLoyaltyPoints)
        {
            decimal total = 0m;
            string notes = string.Empty;

            foreach (var strategy in _strategies)
            {
                var (amount, note) = strategy.Calculate(customer, plan, baseAmount, seatCount, useLoyaltyPoints);
                total += amount;
                notes += note;
            }

            return (total, notes);
        }
    }
}
