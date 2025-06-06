﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DataModel
{
    public class WikiEvent
    {
        [Key]
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

        public List<Person> PeopleAsOldAs(int days)
        {
            return People.Where(x => Day.DayNumber - x.BirthDate.DayNumber == days).ToList();
        }

        public bool HasPeopleOfAge(int days) => PeopleAsOldAs(days).Count > 0;

        public WikiEvent CopyWithPeopleOfAge(int ageInDays)
        {
            return new WikiEvent
            {
                Day = Day,
                Description = Description,
                Id = Id,
                People = PeopleAsOldAs(ageInDays)
            };
        }

        public (WikiEvent ActualEvent, int Dist)? CopyWithPeopleOfAgeWithinTolerance(int ageInDays, int tolerance)
        {
            for (int i = ageInDays; i <= ageInDays + tolerance; i++)
            {
                if (HasPeopleOfAge(i))
                {
                    return (CopyWithPeopleOfAge(i), i - ageInDays);
                }
            }
            return null;
        }

        public bool IsNonAdultEvent
        {
            get
            {
                string[] bannedWords = ["kill", "hanged", "dead", "massacre"];
                return bannedWords.All(x => !Description.Contains(x, StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}
