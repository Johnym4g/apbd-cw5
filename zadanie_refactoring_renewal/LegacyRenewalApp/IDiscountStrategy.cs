namespace LegacyRenewalApp
{
    public interface IDiscountStrategy
    {
        (decimal Amount, string Note) Calculate(Customer customer, SubscriptionPlan plan, decimal baseAmount, int seatCount, bool useLoyaltyPoints);
    }
}
