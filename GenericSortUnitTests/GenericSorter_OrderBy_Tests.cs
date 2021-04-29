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
        public void NominalBehavior_NullObjectsFirst_CustomSeparator()
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

        // TODO : failure unit tests
    }
}
