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
        /// The caracter that separates a property from its sub-property.
        /// </summary>
        public static char PropertyTreeSeparator { get; set; } = '.';

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
        /// Property name must be an exact match (but name is trimmed).
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
        /// <exception cref="ArgumentException"><paramref name="propertyNames"/> contains an invalid property name.</exception>
        /// <exception cref="ArgumentException"><paramref name="descPropertyNames"/> contains a property not included into <paramref name="propertyNames"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="errorManagementType"/> is set to <see cref="ErrorManagementType.Throw"/> and <paramref name="propertyNames"/> contains an unknown property.</exception>
        /// <exception cref="ArgumentException"><paramref name="propertyNames"/> contains duplicate.</exception>
        /// <exception cref="ArgumentException"><paramref name="descPropertyNames"/> contains duplicate.</exception>
        public static IReadOnlyCollection<T> OrderBy<T>(
            this IReadOnlyCollection<T> sourceCollection,
            IReadOnlyCollection<string> propertyNames,
            IReadOnlyCollection<string> descPropertyNames,
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
            if (sourceCollection.Count == 0 || propertyNames.Count == 0)
            {
                return sourceCollection;
            }

            var propertyNamesClean = new List<string>(propertyNames.ToTrimmedStrings());

            foreach (var propertyName in propertyNamesClean)
            {
                if (string.IsNullOrWhiteSpace(propertyName)
                    || propertyName.StartsWith(PropertyTreeSeparator)
                    || propertyName.EndsWith(PropertyTreeSeparator)
                    || propertyName.Count(c => c == PropertyTreeSeparator) > 1)
                {
                    throw new ArgumentException($"{nameof(propertyNames)} contains an invalid property name.", nameof(propertyNames));
                }
            }

            CheckForListDuplicate(propertyNamesClean, nameof(propertyNames));

            var descPropertyNamesClean = new List<string>(descPropertyNames.ToTrimmedStrings());

            foreach (var descPropertyName in descPropertyNamesClean)
            {
                if (!propertyNamesClean.Contains(descPropertyName))
                {
                    throw new ArgumentException($"{nameof(descPropertyNames)} contains a property not included into {nameof(propertyNames)}.", nameof(descPropertyNames));
                }
            }
            
            CheckForListDuplicate(descPropertyNamesClean, nameof(descPropertyNames));

            var nullObjectsList = sourceCollection.Where(_ => _ == null).ToList();

            // transforms into IOrderedEnumerable<T> without doing an actual sort
            var sortableObjectsList = sourceCollection.Except(nullObjectsList).OrderBy(_ => 1);

            var isFirstSort = true;
            foreach (var propertyName in propertyNamesClean)
            {
                // this will contain the field to use as a sort
                Func<T, object> sortKeySelector = null;
                if (propertyName.Contains(PropertyTreeSeparator))
                {
                    // field to sort is on a subproperty of a property
                    var propNameComponents = propertyName.Split(PropertyTreeSeparator);
                    var propNameLevel1 = propNameComponents[0];
                    var propNameLevel2 = propNameComponents[1];

                    if (string.IsNullOrWhiteSpace(propNameLevel1) || string.IsNullOrWhiteSpace(propNameLevel2))
                    {
                        throw new ArgumentException($"{nameof(propertyNames)} contains an invalid property name.", nameof(propertyNames));
                    }

                    var propertyLevel1 = typeof(T).GetPropertyInfo(propNameLevel1, errorManagementType, nameof(propertyNames));
                    if (propertyLevel1 == null)
                    {
                        continue;
                    }

                    var propertyLevel2 = propertyLevel1.PropertyType.GetPropertyInfo(propNameLevel2, errorManagementType, nameof(propertyNames));
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
                    var property = typeof(T).GetPropertyInfo(propertyName, errorManagementType, nameof(propertyNames));
                    if (property == null)
                    {
                        continue;
                    }

                    // basic case
                    sortKeySelector = (p) => property.GetValue(p);
                }

                var isDesc = descPropertyNamesClean.Contains(propertyName);

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

            var finalObjectsList = sortableObjectsList.ToList();
            finalObjectsList.AddRange(nullObjectsList);

            // puts null in first or last position
            return finalObjectsList
                .OrderBy(_ => nullFirst ? _ != null : _ == null)
                .ToList();
        }

        private static void CheckForListDuplicate(IReadOnlyCollection<string> sourceList, string parameterName)
        {
            if (sourceList.Count != sourceList.Distinct().Count())
            {
                throw new ArgumentException($"{parameterName} contains duplicate.", parameterName);
            }
        }

        private static PropertyInfo GetPropertyInfo(
            this Type type,
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

        private static IEnumerable<string> ToTrimmedStrings(this IEnumerable<string> stringsList)
        {
            return stringsList.Select(_ => _ == null ? _ : _.Trim());
        }
    }
}
