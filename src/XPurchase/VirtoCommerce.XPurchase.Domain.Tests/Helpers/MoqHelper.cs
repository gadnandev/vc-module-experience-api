using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoMapper;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.MarketingModule.Core.Model.Promotions;
using VirtoCommerce.MarketingModule.Core.Services;
using VirtoCommerce.PaymentModule.Core.Services;
using VirtoCommerce.PricingModule.Core.Model;
using VirtoCommerce.ShippingModule.Core.Services;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.TaxModule.Core.Services;
using VirtoCommerce.XPurchase.Services;

namespace VirtoCommerce.XPurchase.Tests.Helpers
{
    public class MoqHelper
    {
        protected readonly Fixture _fixture = new Fixture();

        protected readonly Mock<ICartProductService> _cartProductServiceMock;
        protected readonly Mock<ICurrencyService> _currencyServiceMock;
        protected readonly Mock<IMarketingPromoEvaluator> _marketingPromoEvaluatorMock;
        protected readonly Mock<IPaymentMethodsSearchService> _paymentMethodsSearchServiceMock;
        protected readonly Mock<IShippingMethodsSearchService> _shippingMethodsSearchServiceMock;
        protected readonly Mock<IShoppingCartTotalsCalculator> _shoppingCartTotalsCalculatorMock;
        protected readonly Mock<IStoreService> _storeServiceMock;
        protected readonly Mock<ITaxProviderSearchService> _taxProviderSearchServiceMock;
        protected readonly Mock<IMapper> _mapperMock;

        private const string CART_NAME = "default";
        private const string CURRENCY_CODE = "USD";
        private const string CULTURE_NAME = "en-US";

        public MoqHelper()
        {
            _fixture.Register(() => new Language(CULTURE_NAME));
            _fixture.Register(() => new Currency(_fixture.Create<Language>(), CURRENCY_CODE));
            _fixture.Register(() => _fixture
                .Build<ShoppingCart>()
                .With(x => x.Currency, CURRENCY_CODE)
                .With(x => x.LanguageCode, CULTURE_NAME)
                .With(x => x.Name, CART_NAME)
                .Without(x => x.Items)
                .Create());
            //_fixture.Register<IMutablePagedList<DynamicProperty>>(() => null);
            //_fixture.Register(() => _fixture.Build<DynamicPropertyName>().With(x => x.Locale, "en-US").Create());
            //_fixture.Register(() => _fixture.Build<DynamicPropertyObjectValue>().With(x => x.Locale, "en-US").Create());
            //_fixture.Register<IMutablePagedList<SettingEntry>>(() => null);
            //_fixture.Register<IMutablePagedList<Contact>>(() => null);
            //_fixture.Register<IMutablePagedList<QuoteRequest>>(() => null);
            //_fixture.Register<IMutablePagedList<Category>>(() => null);
            //_fixture.Register<IMutablePagedList<Product>>(() => null);
            //_fixture.Register<IList<Product>>(() => null);
            //_fixture.Register<IList<ValidationError>>(() => null);
            //_fixture.Register<IMutablePagedList<CatalogProperty>>(() => null);
            //_fixture.Register<IMutablePagedList<ProductAssociation>>(() => null);
            //_fixture.Register<IMutablePagedList<EditorialReview>>(() => null);
            //_fixture.Register(() => _fixture.Build<LineItem>()
            //                                .Without(x => x.DynamicProperties)
            //                                .With(x => x.IsReadOnly, false)
            //                                .Create());
            //_fixture.Register(() => _fixture.Build<ShoppingCart>().Without(x => x.DynamicProperties).Create());
            _fixture.Register<Price>(() => null);

            _cartProductServiceMock = new Mock<ICartProductService>();

            _currencyServiceMock = new Mock<ICurrencyService>();
            _currencyServiceMock
                .Setup(x => x.GetAllCurrenciesAsync())
                .ReturnsAsync(_fixture.CreateMany<Currency>(1).ToList());

            _marketingPromoEvaluatorMock = new Mock<IMarketingPromoEvaluator>();
            _marketingPromoEvaluatorMock
                .Setup(x => x.EvaluatePromotionAsync(It.IsAny<IEvaluationContext>()))
                .ReturnsAsync(new MockedPromotionResult());

            _paymentMethodsSearchServiceMock = new Mock<IPaymentMethodsSearchService>();
            _shippingMethodsSearchServiceMock = new Mock<IShippingMethodsSearchService>();
            _shoppingCartTotalsCalculatorMock = new Mock<IShoppingCartTotalsCalculator>();
            _storeServiceMock = new Mock<IStoreService>();
            _storeServiceMock
                .Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(_fixture.Create<Store>());

            _taxProviderSearchServiceMock = new Mock<ITaxProviderSearchService>();
            _mapperMock = new Mock<IMapper>();
        }

        protected ShoppingCart GetCart() => _fixture.Create<ShoppingCart>();

        protected Currency GetCurrency() => _fixture.Create<Currency>();

        protected Member GetMember() => _fixture.Create<MockedMember>();

        protected Store GetStore() => _fixture.Create<Store>();

        protected NewCartItem BuildNewCartItem(string productId, int quantity, decimal productPrice)
        {
            var catalogProductId = _fixture.Create<string>();

            var catalogProduct = new CatalogModule.Core.Model.CatalogProduct
            {
                Id = catalogProductId
            };

            var cartProduct = new CartProduct(catalogProduct);
            cartProduct.ApplyPrices(new List<Price>()
            {
                new Price
                {
                    ProductId = catalogProductId,
                    PricelistId = _fixture.Create<string>(),
                    List = _fixture.Create<decimal>(),
                    MinQuantity = _fixture.Create<int>(),
                }
            }, GetCurrency());

            var newCartItem = new NewCartItem(productId, quantity)
            {
                Price = productPrice,
                CartProduct = cartProduct
            };

            return newCartItem;
        }
    }

    public class MockedPromotionResult : PromotionResult
    {
        public new ICollection<PromotionReward> Rewards { get => Enumerable.Empty<PromotionReward>().ToList(); }
    }

    public class MockedMember : Member
    {
    }
}