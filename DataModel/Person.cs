using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataModel
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Person
    {
        [JsonProperty]
        [Key]
        public string EntityId { get; set; }
        [JsonProperty]
        public string FullName { get; set; }
        [JsonProperty]
        public DateOnly BirthDate { get; set; }
        [JsonProperty]
        public DateOnly? DeathDate { get; set; }
        public override string ToString()
        {
            return $"{FullName}, Born: {BirthDate.Day}-{BirthDate.Month}-{BirthDate.Year}, Dead:{(DeathDate.HasValue ? $"{DeathDate.Value.Day}-{DeathDate.Value.Month}-{DeathDate.Value.Year}" : "<alive>")}";
        }
    }
}
