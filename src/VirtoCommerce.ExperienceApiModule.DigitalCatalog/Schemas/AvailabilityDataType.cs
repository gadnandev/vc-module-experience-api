using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.DataLoader;
using GraphQL.Resolvers;
using GraphQL.Types;
using MediatR;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.ExperienceApiModule.Core.Helpers;
using VirtoCommerce.InventoryModule.Core.Model;
using VirtoCommerce.XDigitalCatalog.Queries;

namespace VirtoCommerce.XDigitalCatalog.Schemas
{
    public class AvailabilityDataType : ObjectGraphType
    {
        public AvailabilityDataType(IDataLoaderContextAccessor dataLoader, IMediator mediator)
        {
            Name = "AvailabilityData";

            var quantity = 0;
            var isOnStock = false;


            var inventoriesField = new FieldType
            {
                Name = "inventories",
                Type = GraphTypeExtenstionHelper.GetActualType<ListGraphType<InventoryInfoType>>(),
                Resolver = new AsyncFieldResolver<InventoryInfo, object>(async context =>
                {

                    return new List<InventoryInfo>();
                })
            };
            AddField(inventoriesField);

            var quantityField = new FieldType
            {
                Name = "availableQuantity",
                Type = GraphTypeExtenstionHelper.GetActualType<LongGraphType>(),
                Resolver = new AsyncFieldResolver<long, object>(async context =>  quantity)
            };
            AddField(quantityField);

            var inStockField = new FieldType
            {
                Name = "isOnStock",
                Type = GraphTypeExtenstionHelper.GetActualType<BooleanGraphType>(),
                Resolver = new AsyncFieldResolver<bool, object>(async context => isOnStock)
            };
            AddField(quantityField);


        }



    }
}
