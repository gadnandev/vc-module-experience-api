using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.CatalogModule.Core.Model.Search;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.ExperienceApiModule.DigitalCatalog.Index;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.ExperienceApiModule.Core.Index
{
    public class SearchRequestBuilder
    {
        private ISearchPhraseParser _phraseParser;
        private readonly IAggregationConverter _aggregationConverter;

        private SearchRequest SearchRequest { get; set; }

        public SearchRequestBuilder(ISearchPhraseParser phraseParser, IAggregationConverter aggregationConverter)
            : this()
        {
            _phraseParser = phraseParser;
            _aggregationConverter = aggregationConverter;
        }

        public SearchRequestBuilder()
        {
            SearchRequest = new SearchRequest
            {
                Filter = new AndFilter()
                {
                    ChildFilters = new List<IFilter>(),
                },
                SearchFields = new List<string> { "__content" },
                Sorting = new List<SortingField> { new SortingField("__sort") },
                Skip = 0,
                Take = 20,
                Aggregations = new List<AggregationRequest>(),
            };
        }

        public virtual SearchRequest Build()
        {
            //Apply multi-select facet search policy by default
            foreach (var aggr in SearchRequest.Aggregations)
            {
                var clonedFilter = SearchRequest.Filter.Clone() as AndFilter;
                clonedFilter.ChildFilters = clonedFilter.ChildFilters.Where(x => !(x is INamedFilter namedFilter) || !namedFilter.FieldName.EqualsInvariant(aggr.FieldName)).ToList();
                aggr.Filter = clonedFilter;
            }
            return SearchRequest;
        }

        public SearchRequestBuilder WithFuzzy(bool fuzzy, int? fuzzyLevel)
        {
            SearchRequest.IsFuzzySearch = fuzzy;
            SearchRequest.Fuzziness = fuzzyLevel;
            return this;
        }

        public SearchRequestBuilder WithPaging(int skip, int take)
        {
            SearchRequest.Skip = skip;
            SearchRequest.Take = take;
            return this;
        }

        public SearchRequestBuilder WithSearchPhrase(string searchPhrase)
        {
            SearchRequest.SearchKeywords = searchPhrase;
            return this;
        }

        public SearchRequestBuilder WithPhraseParser(ISearchPhraseParser phraseParser)
        {
            _phraseParser = phraseParser;
            return this;
        }

        public SearchRequestBuilder WithIncludeFields(params string[] includeFields)
        {
            if (SearchRequest.IncludeFields == null)
            {
                SearchRequest.IncludeFields = new List<string>() { };
            }
            SearchRequest.IncludeFields.AddRange(includeFields);
            return this;
        }

        public SearchRequestBuilder AddTerms(IEnumerable<string> terms)
        {
            if (terms != null)
            {
                const string commaEscapeString = "%x2C";

                var nameValueDelimeter = new[] { ':' };
                var valuesDelimeter = new[] { ',' };

                var termsFields = terms.Select(item => item.Split(nameValueDelimeter, 2))
                        .Where(item => item.Length == 2)
                        .Select(item => new TermFilter
                        {
                            FieldName = item[0],
                            Values = item[1].Split(valuesDelimeter, StringSplitOptions.RemoveEmptyEntries)
                                .Select(x => x?.Replace(commaEscapeString, ","))
                                .ToArray()
                        }).ToArray();
                ((AndFilter)SearchRequest.Filter).ChildFilters.AddRange(termsFields);
            }
            return this;
        }

        public SearchRequestBuilder ParseFilters(string filterPhrase)
        {
            if (filterPhrase == null)
            {
                return this;
            }
            if (_phraseParser == null)
            {
                throw new OperationCanceledException("phrase parser must be initialized");
            }
            var parseResult = _phraseParser.Parse(filterPhrase);
            var filters = new List<IFilter>();
            foreach (var filter in parseResult.Filters)
            {
                FilterSyntaxMapper.MapFilterAdditionalSyntax(filter);
                if (filter is TermFilter termFilter)
                {
                    var wildcardValues = termFilter.Values.Where(x => new[] { "?", "*" }.Any(x.Contains));
                    if (wildcardValues.Any())
                    {
                        var orFilter = new OrFilter
                        {
                            ChildFilters = new List<IFilter>()
                        };
                        var wildcardTermFilters = wildcardValues.Select(x => new WildCardTermFilter { FieldName = termFilter.FieldName, Value = x }).ToList();
                        orFilter.ChildFilters.AddRange(wildcardTermFilters);
                        termFilter.Values = termFilter.Values.Except(wildcardValues).ToList();
                        if (termFilter.Values.Any())
                        {
                            orFilter.ChildFilters.Add(termFilter);
                        }
                        filters.Add(orFilter);
                    }
                    else
                    {
                        filters.Add(termFilter);
                    }
                }
                else
                {
                    filters.Add(filter);
                }
            }
            ((AndFilter)SearchRequest.Filter).ChildFilters.AddRange(filters);

            return this;
        }

        public SearchRequestBuilder ParseFacets(string facetPhrase, string storeId = null)
        {
            if (string.IsNullOrWhiteSpace(facetPhrase))
            {
                if (!string.IsNullOrEmpty(storeId) && _aggregationConverter != null)
                {
                    // TODO: Add izolation from store
                    // Maybe we need to implement ProductSearchRequestBuilder.BuildRequestAsync to fill FilterContainer correctly?
                    SearchRequest.Aggregations = _aggregationConverter.GetAggregationRequestsAsync(new ProductIndexedSearchCriteria
                    {
                        StoreId = storeId
                    }, new FiltersContainer()).GetAwaiter().GetResult();
                }

                return this;
            }

            if (_phraseParser == null)
            {
                throw new OperationCanceledException("phrase parser must be initialized");
            }

            //TODO: Support aliases for Facet expressions e.g price.usd[TO 200) as price_below_200
            //TODO: Need to create a new  Antlr file with g4-lexer rules and generate parser especially for facets expression that will return proper AggregationRequests objects
            var parseResult = _phraseParser.Parse(facetPhrase);
            var aggrs = new List<AggregationRequest>();
            //Term facets
            if (!string.IsNullOrEmpty(parseResult.Keyword))
            {
                var termFacetExpressions = parseResult.Keyword.Split(" ");
                parseResult.Filters.AddRange(termFacetExpressions.Select(x => new TermFilter { FieldName = x, Values = new List<string>() }));
            }

            foreach (var filter in parseResult.Filters)
            {
                FilterSyntaxMapper.MapFilterAdditionalSyntax(filter);
                //Range facets
                if (filter is RangeFilter rangeFilter)
                {
                    var rangeAggrRequest = new RangeAggregationRequest
                    {
                        Id = filter.Stringify(),
                        FieldName = rangeFilter.FieldName,
                        Values = rangeFilter.Values.Select(x => new RangeAggregationRequestValue
                        {
                            Id = x.Stringify(),
                            Lower = x.Lower,
                            Upper = x.Upper,
                            IncludeLower = x.IncludeLower,
                            IncludeUpper = x.IncludeUpper
                        }).ToList()
                    };
                    aggrs.Add(rangeAggrRequest);
                }
                //Filter facets
                if (filter is TermFilter termFilter)
                {
                    aggrs.Add(new TermAggregationRequest { FieldName = termFilter.FieldName, Id = filter.Stringify(), Filter = termFilter });
                }
            }

            SearchRequest.Aggregations = aggrs;

            return this;
        }

        public SearchRequestBuilder AddObjectIds(IEnumerable<string> ids)
        {
            if (!ids.IsNullOrEmpty())
            {
                ((AndFilter)SearchRequest.Filter).ChildFilters.Add(new IdsFilter { Values = ids.ToArray() });
            }

            return this;
        }

        public SearchRequestBuilder AddSorting(string sort)
        {
            //TODO: How to sort by scoring relevance???
            //TODO: Alias replacement for sort fields as well as for filter and facet expressions
            var sortFields = new List<SortingField>();
            foreach (var sortInfo in SortInfo.Parse(sort))
            {
                var sortingField = new SortingField();
                if (sortInfo is GeoSortInfo geoSortInfo)
                {
                    sortingField = new GeoDistanceSortingField
                    {
                        Location = geoSortInfo.GeoPoint
                    };
                }
                sortingField.FieldName = sortInfo.SortColumn.ToLowerInvariant();
                sortingField.IsDescending = sortInfo.SortDirection == SortDirection.Descending;

                switch (sortingField.FieldName)
                {
                    case "name":
                    case "title":
                        sortFields.Add(new SortingField("name", sortingField.IsDescending));
                        break;

                    default:
                        sortFields.Add(sortingField);
                        break;
                }
            }

            if (sortFields.Any())
            {
                SearchRequest.Sorting = sortFields;
            }

            return this;
        }
    }
}