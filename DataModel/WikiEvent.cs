using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DataModel
{
    public class WikiEvent
    {
        public string Id { get; set; } = string.Empty;
        public DateOnly Day { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<Person> People { get; set; } = [];

        public override string ToString()
        {
            return $"{Day.Day}-{Day.Month}-{Day.Year} {Environment.NewLine} {Description}{Environment.NewLine} {string.Join(Environment.NewLine, People)}";
        }

        public static string GenerateId(DateOnly day, int count)
        {
            return GetHashString($"{day.Year}{day.Month}{day.Day}{count}");
        }

        public static string GetHashString(string inputString)
        {
            StringBuilder sb = new();
            foreach (byte b in GetHash(inputString))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }

        public static byte[] GetHash(string inputString)
        {
            using HashAlgorithm algorithm = SHA256.Create();
            return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        public bool AddPerson(Person person)
        {
            if (!People.Any(x => x.EntityId == person.EntityId) && person.BirthDate.CompareTo(Day) <= 0 && (person.DeathDate == null || person.DeathDate.Value.CompareTo(Day) >= 0))
            {
                People.Add(person);
                return true;
            }
            return false;
        }
    }
}
