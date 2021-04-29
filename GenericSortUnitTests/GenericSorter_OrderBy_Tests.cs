using System;
using System.Collections.Generic;
using System.Linq;
using GenericSort;
using Xunit;

namespace GenericSortUnitTests
{
    public class GenericSorter_OrderBy_Tests
    {
        private static Random _randomizer = new Random();

        [Fact]
        public void NominalBehavior_NullObjectsFirst_CustomSeparator_IgnoreFakeProperties()
        {
            // Arrange
            GenericSorter.PropertyTreeSeparator = '$';

            var datas = Enumerable
                .Range(0, 1000)
                .Select(i => _randomizer.Next(0, 200) == 0
                    ? null
                    : FakePeople.CreateNew(_randomizer, i, false))
                .ToList();

            var properties = new List<string>
            {
                nameof(FakePeople.IsGirl),
                nameof(FakePeople.Mother) + GenericSorter.PropertyTreeSeparator + nameof(FakePeople.Salary),
                nameof(FakePeople.Name),
                nameof(FakePeople.DateOfBirth),
                "NotExistingProperty"
            };
            var descProperties = new List<string>
            {
                nameof(FakePeople.IsGirl),
                nameof(FakePeople.DateOfBirth),
                "NotExistingProperty"
            };

            var expected = datas
                .Where(_ => _ != null)
                .OrderBy(_ => 1)
                .ThenByDescending(_ => _.IsGirl)
                .ThenBy(_ => _.Mother == null ? null : (object)_.Mother.Salary)
                .ThenBy(_ => _.Name)
                .ThenByDescending(_ => _.DateOfBirth)
                .ToList();

            expected.AddRange(datas.Where(_ => _ == null));

            expected = expected.OrderBy(_ => _ != null).ToList();

            // Act
            var actualDatas = datas.OrderBy(properties, descProperties, nullObjectsFirst: true).ToList();

            // Assert
            for (var i = 0; i < actualDatas.Count; i++)
            {
                if (expected[i] == null)
                {
                    Assert.Null(actualDatas[i]);
                }
                else
                {
                    Assert.NotNull(actualDatas[i]);
                    Assert.Equal(expected[i].Id, actualDatas[i].Id);
                }
            }
        }

        [Fact]
        public void NominalBehavior_NullValuesLast_DefaultSeparator()
        {
            // Arrange
            var datas = Enumerable
                .Range(0, 1000)
                .Select(i => _randomizer.Next(0, 200) == 0
                    ? null
                    : FakePeople.CreateNew(_randomizer, i, false))
                .ToList();

            var properties = new List<string>
            {
                nameof(FakePeople.IsGirl),
                nameof(FakePeople.Mother) + GenericSorter.PropertyTreeSeparator + nameof(FakePeople.Salary),
                nameof(FakePeople.Name),
                nameof(FakePeople.DateOfBirth)
            };
            var descProperties = new List<string>
            {
                nameof(FakePeople.IsGirl),
                nameof(FakePeople.DateOfBirth)
            };

            var expected = datas
                .Where(_ => _ != null)
                .OrderBy(_ => 1)
                .ThenByDescending(_ => _.IsGirl)
                .ThenBy(_ => _.Mother == null)
                .ThenBy(_ => _.Mother == null ? null : (object)_.Mother.Salary)
                .ThenBy(_ => _.Name == null)
                .ThenBy(_ => _.Name)
                .ThenByDescending(_ => _.DateOfBirth)
                .ToList();

            expected.AddRange(datas.Where(_ => _ == null));

            expected = expected.OrderBy(_ => _ == null).ToList();

            // Act
            var actualDatas = datas.OrderBy(properties, descProperties, nullValuesAlwaysLast: true).ToList();

            // Assert
            for (var i = 0; i < actualDatas.Count; i++)
            {
                if (expected[i] == null)
                {
                    Assert.Null(actualDatas[i]);
                }
                else
                {
                    Assert.NotNull(actualDatas[i]);
                    Assert.Equal(expected[i].Id, actualDatas[i].Id);
                }
            }
        }

        [Fact]
        public void NominalBehavior_SourceItemsListIsEmpty_DoNothing()
        {
            // Arrange
            var items = new List<FakePeople>();

            // Act
            var result = items.OrderBy(
                new List<string> { nameof(FakePeople.DateOfBirth) },
                new List<string>());

            // Assert
            Assert.Equal(items, result); // same reference
        }

        [Fact]
        public void NominalBehavior_PropertyNamesListIsNull_DoesNothing()
        {
            // Arrange
            var items = new List<FakePeople> { new FakePeople() };

            // Act
            var result = items.OrderBy(
                null,
                new List<string>());

            // Assert
            Assert.Equal(items, result); // same reference
        }

        [Fact]
        public void NominalBehavior_PropertyNamesListIsEmpty_DoesNothing()
        {
            // Arrange
            var items = new List<FakePeople> { new FakePeople() };

            // Act
            var result = items.OrderBy(
                new List<string>(),
                new List<string>());

            // Assert
            Assert.Equal(items, result); // same reference
        }

        [Fact]
        public void ErrorBehavior_SourceItemsListIsNull_ThrowsException()
        {
            // Arrange
            List<FakePeople> items = null;

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() =>
                items.OrderBy(new List<string>(), new List<string>()));

            // Assert
            Assert.Equal("sourceCollection", ex.ParamName);
        }

        [Theory]
        [InlineData(" ")]
        [InlineData(" $Bidule")]
        [InlineData("Machin$ ")]
        [InlineData("truc$bidule$machin")]
        public void ErrorBehavior_InvalidPropertyName_ThrowsException(string propertyName)
        {
            // Arrange
            var items = new List<FakePeople> { new FakePeople() };

            GenericSorter.PropertyTreeSeparator = '$';

            // Act
            var ex = Assert.Throws<ArgumentException>(() =>
                items.OrderBy(
                    new List<string> { propertyName },
                    new List<string>()));

            // Assert
            Assert.Equal("propertyNames", ex.ParamName);
            Assert.StartsWith(GenericSorter.GetInvalidPropertyNameMessage("propertyNames"), ex.Message);
        }

        [Fact]
        public void ErrorBehavior_DescPropertyNameNotInPropertyNamesList_ThrowsException()
        {
            // Arrange
            var items = new List<FakePeople> { new FakePeople() };

            // Act
            var ex = Assert.Throws<ArgumentException>(() =>
                items.OrderBy(
                    new List<string> { "bidule" },
                    new List<string> { "machin" }));

            // Assert
            Assert.Equal("descPropertyNames", ex.ParamName);
            Assert.StartsWith(GenericSorter.GetNotIncludedPropertyNameMessage("descPropertyNames", "propertyNames"), ex.Message);
        }

        [Fact]
        public void ErrorBehavior_PropertyNamesListContainsDuplicate_ThrowsException()
        {
            // Arrange
            var items = new List<FakePeople> { new FakePeople() };

            // Act
            var ex = Assert.Throws<ArgumentException>(() =>
                items.OrderBy(
                    new List<string> { "bidule ", " bidule" },
                    new List<string>()));

            // Assert
            Assert.Equal("propertyNames", ex.ParamName);
            Assert.StartsWith(GenericSorter.GetDuplicatePropertiesMessage("propertyNames"), ex.Message);
        }

        [Fact]
        public void ErrorBehavior_DescPropertyNamesListContainsDuplicate_ThrowsException()
        {
            // Arrange
            var items = new List<FakePeople> { new FakePeople() };

            // Act
            var ex = Assert.Throws<ArgumentException>(() =>
                items.OrderBy(
                    new List<string> { "bidule" },
                    new List<string> { "bidule ", " bidule" }));

            // Assert
            Assert.Equal("descPropertyNames", ex.ParamName);
            Assert.StartsWith(GenericSorter.GetDuplicatePropertiesMessage("descPropertyNames"), ex.Message);
        }

        [Fact]
        public void ErrorBehavior_PropertyDoesntExist_ThrowModeEnabled_ThrowsException()
        {
            // Arrange
            var items = new List<FakePeople> { new FakePeople() };

            // Act
            var ex = Assert.Throws<ArgumentException>(() =>
                items.OrderBy(
                    new List<string> { "bidule" },
                    new List<string>(),
                    errorManagementType: ErrorManagementType.Throw));

            // Assert
            Assert.Equal("propertyNames", ex.ParamName);
            Assert.StartsWith(GenericSorter.GetUnknownPropertyMessage("bidule", "propertyNames"), ex.Message);
        }

        [Fact]
        public void ErrorBehavior_SubPropertyDoesntExist_ThrowModeEnabled_ThrowsException()
        {
            // Arrange
            var items = new List<FakePeople> { new FakePeople() };

            // Act
            var ex = Assert.Throws<ArgumentException>(() =>
                items.OrderBy(
                    new List<string> { nameof(FakePeople.Mother) + GenericSorter.PropertyTreeSeparator + "bidule" },
                    new List<string>(),
                    errorManagementType: ErrorManagementType.Throw));

            // Assert
            Assert.Equal("propertyNames", ex.ParamName);
            Assert.StartsWith(GenericSorter.GetUnknownPropertyMessage("bidule", "propertyNames"), ex.Message);
        }
    }
}
