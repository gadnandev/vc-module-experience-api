using System;
using System.Linq;
using GraphQL;
using VirtoCommerce.ExperienceApiModule.Core.Extensions;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.ExperienceApiModule.Core.Index
{
    public static class IFilterExtensions
    {
        public static object MapTo(this TermFilter termFilter, object obj)
        {
            var propertyInfo = obj.GetType().GetProperty(termFilter.FieldName.ToPascalCase());
            if (propertyInfo != null)
            {
                object value = termFilter.Values;
                if (value != null)
                {
                    if (propertyInfo.PropertyType.IsArray)
                    {
                        var elementType = propertyInfo.PropertyType.GetElementType();
                        var actualValues = Array.CreateInstance(elementType, termFilter.Values.Count);
                        for (var i = 0; i < termFilter.Values.Count; i++)
                        {
                            actualValues.SetValue(termFilter.Values[i].ChangeType(elementType), i);
                        }
                        value = actualValues;
                    }
                    else
                    {
                        value = termFilter.Values.FirstOrDefault().ChangeType(propertyInfo.PropertyType);
                    }
                }
                propertyInfo.SetValue(obj, value, null);
            }
            return obj;
        }

        public static string Stringify(this IFilter filter)
        {
            var result = filter.ToString();
            if (filter is RangeFilter rangeFilter)
            {
                result = rangeFilter.FieldName + "_" + string.Join("_", rangeFilter.Values.Select(Stringify));
            }
            else if (filter is TermFilter termFilter)
            {
                result = termFilter.FieldName + (!termFilter.Values.IsNullOrEmpty() ? string.Join("_", termFilter.Values) : string.Empty);
            }
            return result;
        }

        public static string Stringify(this RangeFilterValue rageValue)
        {
            return (rageValue.Lower ?? "*") + "-" + (rageValue.Upper ?? "*");
        }
    }
}
