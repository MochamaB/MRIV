using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Reflection;

namespace MRIV.Filters
{
    public static class FilterExtensions
    {
        // Generic search: checks all string properties for the search term
        public static IQueryable<T> ApplySearchFilter<T>(this IQueryable<T> query, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return query;

            var stringProperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType == typeof(string));

            if (!stringProperties.Any())
                return query;

            // Build a dynamic OR expression for all string properties
            var predicate = string.Join(" || ", stringProperties.Select(p => $"{p.Name} != null && {p.Name}.ToLower().Contains(@0)"));
            return query.Where(predicate, searchTerm.ToLower());
        }

        // Generic sorting by property name
        public static IQueryable<T> ApplySorting<T>(this IQueryable<T> query, string sortColumn, bool sortDescending)
        {
            if (string.IsNullOrWhiteSpace(sortColumn))
                return query;
            var direction = sortDescending ? "descending" : "ascending";
            return query.OrderBy($"{sortColumn} {direction}");
        }
    }
}
