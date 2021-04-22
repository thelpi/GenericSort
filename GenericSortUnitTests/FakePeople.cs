using System;

namespace GenericSortUnitTests
{
    public class FakePeople
    {
        private static readonly string[] Names = new[] { "Riri", "Fifi", "Loulou", "Donald", "Picsou", "Rapetout", "Daisy" };

        public bool IsGirl { get; private set; }
        public string Name { get; private set; }
        public DateTime DateOfBirth { get; private set; }
        public FakePeople Mother { get; private set; }
        public double Salary { get; private set; }
        public int Id { get; private set; }

        public static FakePeople CreateNew(Random randomizer, int i)
        {
            return new FakePeople
            {
                DateOfBirth = DateTime.Now.AddDays(randomizer.Next(0, 20)),
                IsGirl = randomizer.Next(0, 2) == 0,
                Mother = randomizer.Next(0, 5) == 0 || i == -1
                    ? null
                    : CreateNew(randomizer, -1),
                Name = Names[randomizer.Next(0, Names.Length)],
                Salary = randomizer.Next(0, 50),
                Id = i
            };
        }
    }
}
