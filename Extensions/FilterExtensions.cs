using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MRIV.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace MRIV.Extensions
{
    public static class FilterExtensions
    {
        /// <summary>
        /// Creates a FilterViewModel with dynamic filter options based on the provided queryable data source
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="query">The queryable data source</param>
        /// <param name="propertiesToFilter">Expression to select which properties to create filters for</param>
        /// <param name="currentFilters">Dictionary of current filter values from request</param>
        /// <returns>A FilterViewModel with populated filter options</returns>
        public static async Task<FilterViewModel> CreateFiltersAsync<T>(
            this IQueryable<T> query,
            Expression<Func<T, object>>[] propertiesToFilter,
            Dictionary<string, string> currentFilters = null)
        {
            var model = new FilterViewModel();
            currentFilters ??= new Dictionary<string, string>();
            
            foreach (var propertySelector in propertiesToFilter)
            {
                // Get property info from the expression
                var memberExpression = GetMemberExpression(propertySelector.Body);
                if (memberExpression == null) continue;
                
                var propertyInfo = memberExpression.Member as PropertyInfo;
                if (propertyInfo == null) continue;
                
                // Get property name and display name
                var propertyName = propertyInfo.Name;
                var displayAttribute = propertyInfo.GetCustomAttribute<DisplayAttribute>();
                var displayName = displayAttribute?.Name ?? propertyInfo.Name;
                
                // Create filter definition
                var filter = new FilterDefinition
                {
                    PropertyName = propertyName,
                    DisplayName = displayName,
                    Options = new List<SelectListItem>()
                };
                var propertyType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
                // Special handling for enum types
                if (propertyType.IsEnum)
                {
                    Console.WriteLine($"Processing enum: {propertyName}");
                    // Get all possible enum values
                    var enumValues = Enum.GetValues(propertyType);

                    foreach (var enumValue in enumValues)
                    {
                        // Get the enum field and its description attribute
                        var enumField = propertyType.GetField(enumValue.ToString());
                        var descriptionAttribute = enumField?.GetCustomAttribute<DescriptionAttribute>();
                        
                        // Use description if available, otherwise use the enum name
                        var displayText = descriptionAttribute?.Description ?? enumValue.ToString();
                        
                        // Use the integer value for filtering
                        var intValue = Convert.ToInt32(enumValue).ToString();
                        Console.WriteLine($"  {enumValue} => Value: {intValue}, Text: {displayText}");

                        var isSelected = currentFilters.TryGetValue(propertyName, out var currentValue) && 
                                        currentValue == intValue;
                        
                        filter.Options.Add(new SelectListItem
                        {
                            Value = intValue,
                            Text = displayText,
                            Selected = isSelected
                        });
                    }
                }
                else
                {
                    Console.WriteLine($"Processing others: {propertyName}");
                    // Get distinct values for this property
                    var values = await query
                        .Select(e => EF.Property<object>(e, propertyName))
                        .Where(v => v != null)
                        .Distinct()
                        .Take(100) // Limit to prevent performance issues
                        .ToListAsync();
                    
                    // Create select list items for non-enum types
                    foreach (var value in values.OrderBy(v => v.ToString()))
                    {
                        var stringValue = value.ToString();
                        var isSelected = currentFilters.TryGetValue(propertyName, out var currentValue) && 
                                        currentValue == stringValue;
                        
                        filter.Options.Add(new SelectListItem
                        {
                            Value = stringValue,
                            Text = stringValue,
                            Selected = isSelected
                        });
                    }
                }
                
                // Only add the filter if it has options
                if (filter.Options.Any())
                {
                    model.Filters.Add(filter);
                }
            }
            
            return model;
        }

        /// <summary>
        /// Applies filters from the request to a queryable data source
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="query">The queryable data source</param>
        /// <param name="filters">Dictionary of filter values from request</param>
        /// <returns>Filtered queryable</returns>
        public static IQueryable<T> ApplyFilters<T>(
      this IQueryable<T> query,
      Dictionary<string, string> filters)
        {
            if (filters == null || !filters.Any())
                return query;

            var entityType = typeof(T);
            var parameter = Expression.Parameter(entityType, "e");

            foreach (var filter in filters.Where(f => !string.IsNullOrEmpty(f.Value)))
            {
                var property = entityType.GetProperty(filter.Key);
                if (property == null) continue;

                var propertyAccess = Expression.Property(parameter, property);
                var filterValue = filter.Value;

                // Create comparison based on property type
                Expression comparison;
                if (property.PropertyType == typeof(string))
                {
                    // String comparison (case-insensitive)
                    var valueExpression = Expression.Constant(filterValue);
                    comparison = Expression.Call(
                        propertyAccess,
                        typeof(string).GetMethod("Contains", new[] { typeof(string) }),
                        valueExpression);
                }
                else if (property.PropertyType.IsEnum ||
                        (Nullable.GetUnderlyingType(property.PropertyType)?.IsEnum == true))
                {
                    // Handle both regular and nullable enums
                    var enumType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                    if (int.TryParse(filterValue, out int intValue))
                    {
                        var enumValue = Enum.ToObject(enumType, intValue);
                        // Create constant with correct type (important for nullable enums)
                        var valueExpression = Expression.Constant(enumValue, property.PropertyType);
                        comparison = Expression.Equal(propertyAccess, valueExpression);
                    }
                    else
                    {
                        continue; // Skip if conversion fails
                    }
                }
                else
                {
                    // Try to convert value to property type
                    object typedValue;
                    try
                    {
                        typedValue = Convert.ChangeType(filterValue, property.PropertyType);
                    }
                    catch
                    {
                        continue; // Skip if conversion fails
                    }

                    // Equality comparison
                    var valueExpression = Expression.Constant(typedValue);
                    comparison = Expression.Equal(propertyAccess, valueExpression);
                }

                // Create lambda for Where clause
                var lambda = Expression.Lambda<Func<T, bool>>(comparison, parameter);

                // Apply filter
                query = query.Where(lambda);
            }

            return query;
        }

        private static MemberExpression GetMemberExpression(Expression expression)
        {
            if (expression is MemberExpression memberExpression)
            {
                return memberExpression;
            }
            
            if (expression is UnaryExpression unaryExpression)
            {
                return GetMemberExpression(unaryExpression.Operand);
            }
            
            if (expression is LambdaExpression lambdaExpression)
            {
                return GetMemberExpression(lambdaExpression.Body);
            }
            
            if (expression is MethodCallExpression methodCallExpression)
            {
                return GetMemberExpression(methodCallExpression.Object);
            }
            
            return null;
        }
    }
}
