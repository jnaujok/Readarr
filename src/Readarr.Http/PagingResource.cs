using System;
using System.Collections.Generic;
using System.ComponentModel;
using NzbDrone.Core.Datastore;

namespace Readarr.Http
{
    public class PagingRequestResource
    {
        public const int MaxPageSize = 250;

        [DefaultValue(1)]
        public int? Page { get; set; }
        [DefaultValue(10)]
        public int? PageSize { get; set; }
        public string SortKey { get; set; }
        public SortDirection? SortDirection { get; set; }
    }

    public class PagingResource<TResource>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public string SortKey { get; set; }
        public SortDirection SortDirection { get; set; }
        public int TotalRecords { get; set; }
        public List<TResource> Records { get; set; }

        public PagingResource()
        {
        }

        public PagingResource(PagingRequestResource requestResource)
        {
            Page = Math.Max(requestResource.Page ?? 1, 1);
            PageSize = Math.Clamp(requestResource.PageSize ?? 10, 1, PagingRequestResource.MaxPageSize);
            SortKey = requestResource.SortKey;
            SortDirection = requestResource.SortDirection ?? SortDirection.Descending;
        }
    }

    public static class PagingResourceMapper
    {
        public static PagingSpec<TModel> MapToPagingSpec<TResource, TModel>(this PagingResource<TResource> pagingResource, string defaultSortKey = "Id", SortDirection defaultSortDirection = SortDirection.Ascending)
        {
            var pagingSpec = new PagingSpec<TModel>
            {
                Page = pagingResource.Page,
                PageSize = pagingResource.PageSize,
                SortKey = pagingResource.SortKey,
                SortDirection = pagingResource.SortDirection,
            };

            if (pagingResource.SortKey == null)
            {
                pagingSpec.SortKey = defaultSortKey;
                if (pagingResource.SortDirection == SortDirection.Default)
                {
                    pagingSpec.SortDirection = defaultSortDirection;
                }
            }

            return pagingSpec;
        }
    }
}
