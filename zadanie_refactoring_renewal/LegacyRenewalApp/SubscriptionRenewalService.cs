using System;

namespace LegacyRenewalApp
{
    public class SubscriptionRenewalService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ISubscriptionPlanRepository _planRepository;
        private readonly IBillingGateway _billingGateway;
        private readonly DiscountCalculator _discountCalculator;
        private readonly TaxRateProvider _taxRateProvider;
        private readonly SupportFeeCalculator _supportFeeCalculator;
        private readonly PaymentFeeCalculator _paymentFeeCalculator;

        public SubscriptionRenewalService()
            : this(
                new CustomerRepository(),
                new SubscriptionPlanRepository(),
                new BillingGatewayAdapter(),
                new DiscountCalculator(new IDiscountStrategy[]
                {
                    new SegmentDiscountStrategy(),
                    new LoyaltyYearsDiscountStrategy(),
                    new TeamSizeDiscountStrategy(),
                    new LoyaltyPointsDiscountStrategy()
                }),
                new TaxRateProvider(),
                new SupportFeeCalculator(),
                new PaymentFeeCalculator())
        {
        }

        public SubscriptionRenewalService(
            ICustomerRepository customerRepository,
            ISubscriptionPlanRepository planRepository,
            IBillingGateway billingGateway,
            DiscountCalculator discountCalculator,
            TaxRateProvider taxRateProvider,
            SupportFeeCalculator supportFeeCalculator,
            PaymentFeeCalculator paymentFeeCalculator)
        {
            _customerRepository = customerRepository;
            _planRepository = planRepository;
            _billingGateway = billingGateway;
            _discountCalculator = discountCalculator;
            _taxRateProvider = taxRateProvider;
            _supportFeeCalculator = supportFeeCalculator;
            _paymentFeeCalculator = paymentFeeCalculator;
        }

        public RenewalInvoice CreateRenewalInvoice(
            int customerId,
            string planCode,
            int seatCount,
            string paymentMethod,
            bool includePremiumSupport,
            bool useLoyaltyPoints)
        {
            ValidateInput(customerId, planCode, seatCount, paymentMethod);

            string normalizedPlanCode = planCode.Trim().ToUpperInvariant();
            string normalizedPaymentMethod = paymentMethod.Trim().ToUpperInvariant();

            var customer = _customerRepository.GetById(customerId);
            var plan = _planRepository.GetByCode(normalizedPlanCode);

            if (!customer.IsActive)
                throw new InvalidOperationException("Inactive customers can't renew subscriptions");

            decimal baseAmount = (plan.MonthlyPricePerSeat * seatCount * 12m) + plan.SetupFee;

            var (discountAmount, notes) = _discountCalculator.Calculate(customer, plan, baseAmount, seatCount, useLoyaltyPoints);

            decimal subtotalAfterDiscount = baseAmount - discountAmount;
            if (subtotalAfterDiscount < 300m)
            {
                subtotalAfterDiscount = 300m;
                notes += "applied minimum discounted subtotal";
            }

            var (supportFee, supportNote) = _supportFeeCalculator.Calculate(normalizedPlanCode, includePremiumSupport);
            notes += supportNote;

            var (paymentFee, paymentNote) = _paymentFeeCalculator.Calculate(normalizedPaymentMethod, subtotalAfterDiscount + supportFee);
            notes += paymentNote;

            decimal taxBase = subtotalAfterDiscount + supportFee + paymentFee;
            decimal taxRate = _taxRateProvider.GetTaxRate(customer.Country);
            decimal taxAmount = taxBase * taxRate;
            decimal finalAmount = taxBase + taxAmount;

            if (finalAmount < 500m)
            {
                finalAmount = 500m;
                notes += "applied minimum invoice amount";
            }

            var invoice = new RenewalInvoice
            {
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{customerId}-{normalizedPlanCode}",
                CustomerName = customer.FullName,
                PlanCode = normalizedPlanCode,
                PaymentMethod = normalizedPaymentMethod,
                SeatCount = seatCount,
                BaseAmount = Math.Round(baseAmount, 2, MidpointRounding.AwayFromZero),
                DiscountAmount = Math.Round(discountAmount, 2, MidpointRounding.AwayFromZero),
                SupportFee = Math.Round(supportFee, 2, MidpointRounding.AwayFromZero),
                PaymentFee = Math.Round(paymentFee, 2, MidpointRounding.AwayFromZero),
                TaxAmount = Math.Round(taxAmount, 2, MidpointRounding.AwayFromZero),
                FinalAmount = Math.Round(finalAmount, 2, MidpointRounding.AwayFromZero),
                Notes = notes.Trim(),
                GeneratedAt = DateTime.UtcNow
            };

            _billingGateway.SaveInvoice(invoice);

            if (!string.IsNullOrWhiteSpace(customer.Email))
            {
                string subject = "Subscription renewal invoice";
                string body = $"Hello {customer.FullName}, your renewal for plan {normalizedPlanCode} has been prepared. Final amount: {invoice.FinalAmount:F2}.";
                _billingGateway.SendEmail(customer.Email, subject, body);
            }

            return invoice;
        }

        private static void ValidateInput(int customerId, string planCode, int seatCount, string paymentMethod)
        {
            if (customerId <= 0)
                throw new ArgumentException("Customer id must be positive");
            if (string.IsNullOrWhiteSpace(planCode))
                throw new ArgumentException("Plan code is required");
            if (seatCount <= 0)
                throw new ArgumentException("Seat count must be positive");
            if (string.IsNullOrWhiteSpace(paymentMethod))
                throw new ArgumentException("Payment method is required");
        }
    }
}
