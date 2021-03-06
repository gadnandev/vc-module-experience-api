using System.Linq;
using GraphQL.Resolvers;
using GraphQL.Types;
using VirtoCommerce.ExperienceApiModule.Core.Extensions;
using VirtoCommerce.ExperienceApiModule.Core.Helpers;
using VirtoCommerce.ExperienceApiModule.Core.Schemas;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XPurchase.Services;

namespace VirtoCommerce.XPurchase.Schemas
{
    public class CartType : ObjectGraphType<CartAggregate>
    {
        public CartType(ICartAvailMethodsService cartAvailMethods)
        {
            Field(x => x.Cart.Id, nullable: true).Description("Shopping cart Id");
            Field(x => x.Cart.Name, nullable: false).Description("Shopping cart name");
            Field(x => x.Cart.Status, nullable: true).Description("Shopping cart status");
            Field(x => x.Cart.StoreId, nullable: true).Description("Shopping cart store id");
            Field(x => x.Cart.ChannelId, nullable: true).Description("Shopping cart channel id");
            Field<BooleanGraphType>("hasPhysicalProducts", resolve: context => AbstractTypeFactory<CartHasPhysicalProductsSpecification>.TryCreateInstance().IsSatisfiedBy(context.Source.Cart));
            Field(x => x.Cart.IsAnonymous, nullable: true).Description("Sign that shopping cart is anonymous");
            //Field(x => x.Customer, nullable: true).Description("Shopping cart user"); //todo: add resolver
            Field(x => x.Cart.CustomerId, nullable: true).Description("Shopping cart user id");
            Field(x => x.Cart.CustomerName, nullable: true).Description("Shopping cart user name");
            Field(x => x.Cart.OrganizationId, nullable: true).Description("Shopping cart organization id");
            Field(x => x.Cart.IsRecuring, nullable: true).Description("Sign that shopping cart is recurring");
            Field(x => x.Cart.Comment, nullable: true).Description("Shopping cart text comment");

            // Characteristics
            Field(x => x.Cart.VolumetricWeight, nullable: true).Description("Shopping cart value of volumetric weight");
            Field(x => x.Cart.WeightUnit, nullable: true).Description("Shopping cart value of weight unit");
            Field(x => x.Cart.Weight, nullable: true).Description("Shopping cart value of shopping cart weight");
            //TODO:
            //Field(x => x.MeasureUnit, nullable: true).Description("Shopping cart value of measurement unit");
            //Field(x => x.Height, nullable: true).Description("Shopping cart value of height");
            //Field(x => x.Length, nullable: true).Description("Shopping cart value of length");
            //Field(x => x.Width, nullable: true).Description("Shopping cart value of width");

            // Money
            Field<MoneyType>("total", resolve: context => context.Source.Cart.Total.ToMoney(context.Source.Currency));
            Field<MoneyType>("subTotal", resolve: context => context.Source.Cart.SubTotal.ToMoney(context.Source.Currency));
            Field<MoneyType>("subTotalWithTax", resolve: context => context.Source.Cart.SubTotalWithTax.ToMoney(context.Source.Currency));
            Field<MoneyType>("extendedPriceTotal", resolve: context => context.Source.Cart.Items.Sum(i => i.ExtendedPrice).ToMoney(context.Source.Currency));
            Field<MoneyType>("extendedPriceTotalWithTax", resolve: context => context.Source.Cart.Items.Sum(i => i.ExtendedPriceWithTax).ToMoney(context.Source.Currency));
            Field<CurrencyType>("currency", resolve: context => context.Source.Currency);
            Field<MoneyType>("taxTotal", resolve: context => context.Source.Cart.TaxTotal.ToMoney(context.Source.Currency));
            Field(x => x.Cart.TaxPercentRate, nullable: true).Description("Tax percent rate");
            Field(x => x.Cart.TaxType, nullable: true).Description("Shipping tax type");
            Field<ListGraphType<TaxDetailType>>("taxDetails", resolve: context => context.Source.Cart.TaxDetails);

            // Shipping
            Field<MoneyType>("shippingPrice", resolve: context => context.Source.Cart.ShippingTotal.ToMoney(context.Source.Currency));
            Field<MoneyType>("shippingPriceWithTax", resolve: context => context.Source.Cart.ShippingTotalWithTax.ToMoney(context.Source.Currency));
            Field<MoneyType>("shippingTotal", resolve: context => context.Source.Cart.ShippingTotal.ToMoney(context.Source.Currency));
            Field<MoneyType>("shippingTotalWithTax", resolve: context => context.Source.Cart.ShippingTotalWithTax.ToMoney(context.Source.Currency));
            //Field<ListGraphType<ShipmentType>>("shipments", resolve: context => context.Source.Cart.Shipments);
            //TODO: By this registration we support the schema types extensions. Need to move this code into extensions and replace everywhere to this version.
            var cartShipmentsField = new FieldType
            {
                Name = "shipments",
                Type = typeof(ListGraphType<>).MakeGenericType(GraphTypeExtenstionHelper.GetActualType<ShipmentType>()),
                Resolver = new FuncFieldResolver<CartAggregate, object>(context => context.Source.Cart.Shipments)
            };
            AddField(cartShipmentsField);

            FieldAsync<ListGraphType<ShippingMethodType>>("availableShippingMethods", resolve: async context =>
            {
                var rates = await cartAvailMethods.GetAvailableShippingRatesAsync(context.Source);
                //store the pair ShippingMethodType and cart aggregate in the user context for future usage in the ShippingMethodType fields resolvers
                if (rates != null)
                {
                    rates.Apply(x => context.UserContext[x.GetCacheKey()] = context.Source);
                }
                return rates;
            });

            // Payment
            Field<MoneyType>("paymentPrice", resolve: context => context.Source.Cart.PaymentTotal.ToMoney(context.Source.Currency));
            Field<MoneyType>("paymentPriceWithTax", resolve: context => context.Source.Cart.PaymentTotalWithTax.ToMoney(context.Source.Currency));
            Field<MoneyType>("paymentTotal", resolve: context => context.Source.Cart.PaymentTotal.ToMoney(context.Source.Currency));
            Field<MoneyType>("paymentTotalWithTax", resolve: context => context.Source.Cart.PaymentTotalWithTax.ToMoney(context.Source.Currency));
            Field<ListGraphType<PaymentType>>("payments", resolve: context => context.Source.Cart.Payments);
            FieldAsync<ListGraphType<PaymentMethodType>>("availablePaymentMethods", resolve: async context =>
            {
                var methods = await cartAvailMethods.GetAvailablePaymentMethodsAsync(context.Source);
                //store the pair ShippingMethodType and cart aggregate in the user context for future usage in the ShippingMethodType fields resolvers
                if (methods != null)
                {
                    methods.Apply(x => context.UserContext[x.Id] = context.Source);
                }
                return methods;
            });
            //TODO:
            //Field<ListGraphType<PaymentPlanType>>("paymentPlan", resolve: context => context.Source.PaymentPlan);

            // Extended money
            //TODO:
            //Field<MoneyType>("extendedPriceTotal", resolve: context => context.Source.ExtendedPriceTotal);
            //Field<MoneyType>("extendedPriceTotalWithTax", resolve: context => context.Source.ExtendedPriceTotalWithTax);

            // Handling totals
            Field<MoneyType>("handlingTotal", resolve: context => context.Source.Cart.HandlingTotal.ToMoney(context.Source.Currency));
            Field<MoneyType>("handlingTotalWithTax", resolve: context => context.Source.Cart.HandlingTotalWithTax.ToMoney(context.Source.Currency));

            // Discounts
            Field<MoneyType>("discountTotal", resolve: context => context.Source.Cart.DiscountTotal.ToMoney(context.Source.Currency));
            Field<MoneyType>("discountTotalWithTax", resolve: context => context.Source.Cart.DiscountTotalWithTax.ToMoney(context.Source.Currency));
            Field<ListGraphType<DiscountType>>("discounts", resolve: context => context.Source.Cart.Discounts);

            // Addresses
            Field<ListGraphType<AddressType>>("addresses", resolve: context => context.Source.Cart.Addresses);

            // Items
            Field<ListGraphType<LineItemType>>("items", resolve: context => context.Source.Cart.Items);

            Field<IntGraphType>("itemsCount", "Count of different items", resolve: context => context.Source.Cart.Items.Count);
            Field<IntGraphType>("itemsQuantity", "Quantity of items", resolve: context => context.Source.Cart.Items.Sum(x => x.Quantity));
            //TODO:
            //Field<LineItemType>("recentlyAddedItem", resolve: context => context.Source.Cart.RecentlyAddedItem);

            // Coupons
            Field<ListGraphType<CouponType>>("coupons", resolve: context => context.Source.Coupons);

            // Other
            //Field<ListGraphType<DynamicPropertyType>>("dynamicProperties", resolve: context => context.Source.DynamicProperties); //todo add dynamic properties
            //TODO:
            Field(x => x.IsValid, nullable: true).Description("Is cart valid");
            Field<ListGraphType<ValidationErrorType>>("validationErrors", resolve: context => context.Source.ValidationErrors.OfType<CartValidationError>());
            Field(x => x.Cart.Type, nullable: true).Description("Shopping cart type");
        }
    }
}
