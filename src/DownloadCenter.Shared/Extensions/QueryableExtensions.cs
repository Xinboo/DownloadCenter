using DownloadCenter.Shared.Models;

namespace DownloadCenter.Shared.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> Page<T>(this IQueryable<T> query, PagedRequest request)
    {
        return query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize);
    }
}
