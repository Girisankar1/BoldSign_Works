namespace BoldSign.Payment.Tests
{
    using System;
    using System.Threading.Tasks;
    using BoldSign.Base;
    using BoldSign.Base.DataClasses;
    using BoldSign.Base.Extensions;
    using BoldSign.Base.Models;
    using BoldSign.Payment.Exceptions;
    using BoldSign.Payment.Models;
    using BoldSign.Payment.Service.DataServices;
    using NUnit.Framework;
    using Stripe;
    using Plan = Base.Models.Plan;
    using Subscription = Base.Models.Subscription;

    public class PopulateSubscriptionProperties
    {
        [OneTimeSetUp]
        public async Task SetUp()
        {
            BoldSignBase.EnvironmentVariables = new EnvironmentVariables()
            {
                LoginAuthority = "https://dev-account.boldsign.com",
                FriendsFamilyPlanId = 83,
                MaxRedemptionLimit = 1,
                InviteLinkExpiryInDays = 365,
                FriendsPlanCouponId = "123cd2",
                FriendsPlanUserCount = 3,
                FriendsPlanDiscountPrice = 360,
            };
        }
        public enum PlanStatus
        {
            incomplete,
            incomplete_expired,
            trialing,
            active,
            past_due,
            canceled,
            unpaid,

        }

        [TestCase(PlanStatus.incomplete)]
        [TestCase(PlanStatus.incomplete_expired)]
        [TestCase(PlanStatus.trialing)]
        [TestCase(PlanStatus.active)]
        [TestCase(PlanStatus.past_due)]
        [TestCase(PlanStatus.canceled)]
        [TestCase(PlanStatus.unpaid)]
        public void SubscriptionPropertiesTest(PlanStatus planStatus)
        {
            var subscriptionDataService = new SubscriptionDataService(null, null, null, null);
            var stripeSub = new Stripe.Subscription()
            {
                StartDate = DateTime.UtcNow,
                EndedAt = DateTime.UtcNow.AddDays(1),
                TrialStart = DateTime.UtcNow,
                TrialEnd = DateTime.UtcNow.AddDays(2),
                CurrentPeriodStart = DateTime.UtcNow,
                CurrentPeriodEnd = DateTime.UtcNow,
                LatestInvoice = new Stripe.Invoice(),
                Id = "sub_1KGwb7Js7zz5FOJ0QkGhmP12",
                CustomerId = "hjjidji",
                Status = "active",


                Items = new Stripe.StripeList<Stripe.SubscriptionItem>()
                {
                    Data = new System.Collections.Generic.List<SubscriptionItem>()
                    {
                        new SubscriptionItem()
                        {
                            Id = "1",
                            Metadata = PaymentHelpers.GetMainSubMetaData(),
                        }
                    },
                },
            };

            var stripePlan = new Stripe.Plan
            {
                Id = "price_1It5bHJs7zz5FOJ0vKcRCrcF",
                Amount = 1200,
            };
            var coupon = new Stripe.Coupon()
            {
                Id = "eqdinwe"
            };
            var discount = new Stripe.Discount()
            {
                PromotionCodeId = new Stripe.Discount().Id

            };
            var subscriptionItem = new Stripe.SubscriptionItem()
            {
                Quantity = 1,
                Plan = new Stripe.Plan()
                
            };
            var options = new PopulateSubscriptionOptions()
            {
                StripeSubscription = stripeSub,
                LatestInvoice = stripeSub.LatestInvoice,
                Plan = new Plan()
                {
                    Id = 78,
                    PlanName = "CustomPlan",
                    ModifiedDate = DateTime.UtcNow,
                    IsActive = true,
                    PlanUserCount = 1,
                    CreatedDate = DateTime.UtcNow,
                    IsCustomPlan = true,
                    AllowADIntegration = true,
                    AllowApiAccess = true,
                    AllowESign = true,
                    AllowForms = true,
                    AllowInPersonSign = true,
                    AllowPrintAndSign = true,
                    AllowTemplates = true,
                    ApiCost = 200,
                    ApiCount = 1,
                    Currency = new Stripe.Plan().Currency,
                    ESignCost = 200,
                    ESignCount = 1,
                    PlanSignCount = 1,
                    PlanType = PlanType.Paid,
                    TemplatesCount = 1,
                    TestRateLimit = 1,
                    ProductionRateLimit = 1,
                    TrialPeriodInDays = 1,
                    UserLimit = 1,
                },
                Subscription = new Subscription()
            };

            var subscription = new Subscription()
            {
                Id = new Guid(),
                Plan = new Plan()
            };
            switch (planStatus)
            {
                case PlanStatus.active:
                    stripeSub.Status = "active";
                    break;

                case PlanStatus.incomplete:
                    stripeSub.Status = "incomplete";
                    break;

                case PlanStatus.incomplete_expired:
                    stripeSub.Status = "incomplete_expired";
                    break;

                case PlanStatus.trialing:
                    stripeSub.Status = "trialing";
                    break;

                case PlanStatus.past_due:
                    stripeSub.Status = "past_due";
                    break;

                case PlanStatus.canceled:
                    stripeSub.Status = "canceled";
                    break;

                case PlanStatus.unpaid:
                    stripeSub.Status = "unpaid";
                    break;


                default: throw new ArgumentOutOfRangeException(nameof(planStatus), planStatus, null);

            }
            

            var result = subscriptionDataService.PopulateSubscriptionProperties(options);

            switch (planStatus)
            {
                case PlanStatus.active:
                    Assert.IsTrue(result.Status == SubscriptionStatus.Active, "Subscription status should be active");

                    break;

                case PlanStatus.incomplete:
                    Assert.IsTrue(result.Status == SubscriptionStatus.PaymentRequired, "Subscription status should be incomplete");

                    break;

                case PlanStatus.incomplete_expired:
                    Assert.IsTrue(result.Status == SubscriptionStatus.SuspendedForPaymentFailure, "Subscription status should be incomplete_expired");

                    break;

                case PlanStatus.trialing:
                    Assert.IsTrue(result.Status == SubscriptionStatus.Trail, "Subscription status should be trialing");

                    break;

                case PlanStatus.past_due:
                    Assert.IsTrue(result.Status == SubscriptionStatus.InGracePeriod, "Subscription status should be past_due");

                    break;

                case PlanStatus.canceled:
                    Assert.IsTrue(result.Status == SubscriptionStatus.Cancelled, "Subscription status should be canceled");

                    break;

                case PlanStatus.unpaid:
                    Assert.IsTrue(result.Status == SubscriptionStatus.PaymentRequired, "Subscription status should be unpaid");

                    break;

                default: throw new ArgumentOutOfRangeException(nameof(planStatus), planStatus, null);
            }

            


            var subscriptionPurchasedUserCount = result.PurchasedUserCount;
            Assert.IsTrue(subscriptionPurchasedUserCount == subscriptionItem.Quantity);

           // Assert.IsTrue(subscriptionPurchasedUserCount == subscriptionItem.Metadata)



            /* Assert.IsTrue(subscriptionProperty != null, "subscriptionProperty should not be null");
             Assert.IsTrue(options.Plan != null, "Plan should not be Null");
             Assert.IsTrue(subscriptionProperty.StartDate.HasValue, "Start date is Now");
             Assert.AreEqual(subscriptionProperty.Id, new Guid());
             Assert.IsTrue(stripePlan.Id != null, "Plan Id not equal to null");
             Assert.IsTrue(coupon.Id != null, "Coupon code should not be null");

             var exception = Assert.CatchAsync<AlreadyInvitedException>(async () => subscriptionDataService.PopulateSubscriptionProperties(options));
             Assert.IsTrue(exception.Message.IsEqual($"The Plan {options.Subscription.Status} was Active"));*/


        }
        [TestCase("1")]
        [TestCase("2")]
        [TestCase("3")]
        public void GetPurchasedUserCountTest(PopulateSubscriptionOptions options)
        {
            var subscriptionDataService = new SubscriptionDataService(null, null, null, null);
            var result = subscriptionDataService.PopulateSubscriptionProperties(options);
           
            var subscriptionPurchasedUserCount = result.PurchasedUserCount;
            Assert.IsTrue(subscriptionPurchasedUserCount == subscriptionItem.Quantity);

        }
    }
}