using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;

namespace VirtoCommerce.ExperienceApiModule.XOrder.Queries
{
    public class GetOrderQuery : IQuery<CustomerOrderAggregate>
    {
        public GetOrderQuery()
        {
        }

        public GetOrderQuery(string orderId, string number)
        {
            OrderId = orderId;
            Number = number;
        }

        public string CultureName { get; set; }
        public string OrderId { get; set; }
        public string Number { get; set; }
    }
}
