namespace LegacyRenewalApp
{
    public class SupportFeeCalculator
    {
        public (decimal Fee, string Note) Calculate(string planCode, bool includePremiumSupport)
        {
            if (!includePremiumSupport)
                return (0m, string.Empty);

            decimal fee = planCode switch
            {
                "START" => 250m,
                "PRO" => 400m,
                "ENTERPRISE" => 700m,
                _ => 0m
            };

            return (fee, "premium support included; ");
        }
    }
}
