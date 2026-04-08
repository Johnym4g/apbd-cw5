namespace LegacyRenewalApp
{
    public class TeamSizeDiscountStrategy : IDiscountStrategy
    {
        public (decimal Amount, string Note) Calculate(Customer customer, SubscriptionPlan plan, decimal baseAmount, int seatCount, bool useLoyaltyPoints)
        {
            if (seatCount >= 50)
                return (baseAmount * 0.12m, "large team discount; ");
            if (seatCount >= 20)
                return (baseAmount * 0.08m, "medium team discount; ");
            if (seatCount >= 10)
                return (baseAmount * 0.04m, "small team discount; ");

            return (0m, string.Empty);
        }
    }
}
