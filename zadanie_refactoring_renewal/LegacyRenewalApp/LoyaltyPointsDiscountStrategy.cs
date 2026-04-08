namespace LegacyRenewalApp
{
    public class LoyaltyPointsDiscountStrategy : IDiscountStrategy
    {
        public (decimal Amount, string Note) Calculate(Customer customer, SubscriptionPlan plan, decimal baseAmount, int seatCount, bool useLoyaltyPoints)
        {
            if (!useLoyaltyPoints || customer.LoyaltyPoints <= 0)
                return (0m, string.Empty);

            int pointsToUse = customer.LoyaltyPoints > 200 ? 200 : customer.LoyaltyPoints;
            return (pointsToUse, $"used loyalty points: {pointsToUse}; ");
        }
    }
}
