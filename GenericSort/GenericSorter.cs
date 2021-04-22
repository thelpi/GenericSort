using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GenericSort
{
    /// <summary>
    /// Helper to sort collections.
    /// </summary>
    public static class GenericSorter
    {
        /// <summary>
        /// Sorts a collection of items using dynamic fields.
        /// </summary>
        /// <typeparam name="T">Targeted type in <paramref name="sourceCollection"/>.</typeparam>
        /// <param name="sourceCollection">Source collection of items.</param>
        /// <param name="propertyNames">
        /// Collection of properties (from <typeparamref name="T"/>) to sort the collection.
        /// If the property is not included in <paramref name="descPropertyNames"/>, the sort is ascending.
        /// The syntax "Property1.Property2" is allowed to sort on a sub-property (property of property).
        /// Sub-property of sub-property is not allowed.
        /// Property name must be an exact match.
        /// Allowed properties are public and not static.
        /// </param>
        /// <param name="descPropertyNames">
        /// Collection of properties on which a descending sort is applied.
        /// Must be included in <paramref name="propertyNames"/>.
        /// </param>
        /// <param name="errorManagementType">
        /// Optionnal.
        /// Sets the behavior in case of error while processing a field.
        /// By default errors are ignored.</param>
        /// <param name="nullFirst">
        /// Optionnal.
        /// Puts <c>null</c> elements in first place if enabled.
        /// Doesn't apply if <typeparamref name="T"/> is not a nullable type.
        /// Doesn't apply for sub-properties.
        /// </param>
        /// <returns>The sorted collection of items.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="descPropertyNames"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="propertyNames"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="sourceCollection"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyNames"/> contains an invalid property name.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="descPropertyNames"/> contains a property not included into <paramref name="propertyNames"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="errorManagementType"/> is set to <see cref="ErrorManagementType.Throw"/> and <paramref name="propertyNames"/> contains an unknown property.
        /// </exception>
        public static IEnumerable<T> OrderBy<T>(
            this IEnumerable<T> sourceCollection,
            IEnumerable<string> propertyNames,
            IEnumerable<string> descPropertyNames,
            ErrorManagementType errorManagementType = ErrorManagementType.Ignore,
            bool nullFirst = false)
        {
            if (sourceCollection == null)
            {
                throw new ArgumentNullException(nameof(sourceCollection));
            }

            if (propertyNames == null)
            {
                throw new ArgumentNullException(nameof(propertyNames));
            }

            if (descPropertyNames == null)
            {
                throw new ArgumentNullException(nameof(descPropertyNames));
            }

            // no sort required
            if (!sourceCollection.Any() || !propertyNames.Any())
            {
                return sourceCollection;
            }

            foreach (var propertyName in propertyNames)
            {
                if (string.IsNullOrWhiteSpace(propertyName)
                    || propertyName.StartsWith('.')
                    || propertyName.EndsWith('.')
                    || propertyName.Count(c => c == '.') > 1)
                {
                    throw new ArgumentException($"{nameof(propertyNames)} contains an invalid property name.", nameof(propertyNames));
                }
            }

            foreach (var descPropertyName in descPropertyNames)
            {
                if (!propertyNames.Contains(descPropertyName))
                {
                    throw new ArgumentException($"{nameof(descPropertyNames)} contains a property not included into {nameof(propertyNames)}.", nameof(descPropertyNames));
                }
            }

            // transforms into IOrderedEnumerable<T> without doing an actual sort
            var sortableObjectsList = sourceCollection.OrderBy(_ => 1);

            var isFirstSort = true;
            foreach (var propertyName in propertyNames)
            {
                // this will contain the field to use as a sort
                Func<T, object> sortKeySelector = null;
                if (propertyName.Contains("."))
                {
                    // field to sort is on a subproperty of a property
                    var propNameLevel1 = propertyName.Split('.')[0];
                    var propNameLevel2 = propertyName.Split('.')[1];

                    if (string.IsNullOrWhiteSpace(propNameLevel1) || string.IsNullOrWhiteSpace(propNameLevel2))
                    {
                        throw new ArgumentException($"{nameof(propertyNames)} contains an invalid property name.", nameof(propertyNames));
                    }

                    var propertyLevel1 = GetPropertyInfo(typeof(T), propNameLevel1, errorManagementType, nameof(propertyNames));
                    if (propertyLevel1 == null)
                    {
                        continue;
                    }

                    var propertyLevel2 = GetPropertyInfo(propertyLevel1.PropertyType, propNameLevel2, errorManagementType, nameof(propertyNames));
                    if (propertyLevel2 == null)
                    {
                        continue;
                    }

                    sortKeySelector = (p) =>
                    {
                        var propValueLevel1 = propertyLevel1.GetValue(p);
                        var propValueLevel2 = propValueLevel1 == null
                            ? null
                            : propertyLevel2.GetValue(propValueLevel1);
                        return propValueLevel2;
                    };
                }
                else
                {
                    var property = GetPropertyInfo(typeof(T), propertyName, errorManagementType, nameof(propertyNames));
                    if (property == null)
                    {
                        continue;
                    }

                    // basic case
                    sortKeySelector = (p) => property.GetValue(p);
                }

                var isDesc = descPropertyNames.Contains(propertyName);

                if (isFirstSort)
                {
                    isFirstSort = false;
                    sortableObjectsList = isDesc
                        ? sortableObjectsList.OrderByDescending(sortKeySelector)
                        : sortableObjectsList.OrderBy(sortKeySelector);
                }
                else
                {
                    sortableObjectsList = isDesc
                        ? sortableObjectsList.ThenByDescending(sortKeySelector)
                        : sortableObjectsList.ThenBy(sortKeySelector);
                }
            }

            if (nullFirst)
            {
                // puts null in first position
                sortableObjectsList = sortableObjectsList.OrderBy(_ => _ != null);
            }

            return sortableObjectsList;
        }

        private static PropertyInfo GetPropertyInfo(
            Type type,
            string propertyName,
            ErrorManagementType errorManagementType,
            string parameterName)
        {
            PropertyInfo property = null;
            try
            {
                property = type.GetProperty(propertyName);
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"GetProperty exception: {ex.Message}");
#endif
                if (errorManagementType == ErrorManagementType.Throw)
                {
                    throw new ArgumentException($"{parameterName} contains an unknown property: {propertyName}.", parameterName);
                }
            }

            return property;
        }
    }
}
