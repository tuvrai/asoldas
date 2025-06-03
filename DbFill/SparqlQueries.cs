using DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DbFill
{
    internal static class SparqlQueries
    {
        internal static async Task<Person?> GetPersonUsingEntityId(string entityId)
        {
            string queryFormat = @"https://query.wikidata.org/sparql?query=SELECT%20%3FisHuman%20%3FbirthDate%20%3FdeathDate%20WHERE%20%7B%0A%20%20OPTIONAL%20%7B%20wd%3A{0}%20wdt%3AP31%20wd%3AQ5.%20BIND(true%20AS%20%3FisHuman)%20%7D%0A%20%20OPTIONAL%20%7B%20wd%3A{0}%20wdt%3AP569%20%3FbirthDate.%20%7D%0A%20%20OPTIONAL%20%7B%20wd%3A{0}%20wdt%3AP570%20%3FdeathDate.%20%7D%0A%7D%0A";
            string actualQuery = string.Format(queryFormat, entityId);

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MyCSharpApp/1.0 (example@example.com)");

            string xmlContent = await httpClient.GetStringAsync(actualQuery);

            XDocument doc = XDocument.Parse(xmlContent);

            Person? person = null;

            if (doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "result") is XElement result)
            {
                if (IsHuman(result))
                {
                    if (GetDate(result, "birthDate") is DateOnly birthDate)
                    {
                        person = new Person()
                        {
                            EntityId = entityId,
                            BirthDate = birthDate,
                            DeathDate = GetDate(result, "deathDate")
                        };
                    }
                }
                
            }
            return person;
        }

        public static DateOnly? GetDate(XElement result, string attrName)
        {
            if (result.Descendants().Where(e => e.Name.LocalName == "binding").FirstOrDefault(x => x.Attribute("name")?.Value == attrName) is XElement elementB
                     && ParseAsDate(elementB) is DateOnly date
                     && date.Year > 0)
            {
                return date;
            }
            else
            {
                return null;
            }
        }

        private static DateOnly? ParseAsDate(XElement element)
        {
            string[] arr = element.Value.Split('T')[0].Split('-');
            try
            {
                return new DateOnly(int.Parse(arr[0]), int.Parse(arr[1]), int.Parse(arr[2]));
            }
            catch
            {
                Logger.Log($"\t\tError: weird date - {element.Value}");
                return null;
            }
        }

        public static bool IsHuman(XElement xElement)
        {
            return xElement.Descendants().Where(e => e.Name.LocalName == "binding")
                    .FirstOrDefault(x => x.Attribute("name")?.Value == "isHuman") is XElement el
                    && !string.IsNullOrEmpty(el.Value)
                    && bool.TryParse(el.Value, out bool isHuman)
                    && isHuman == true;
        }

        public static string? GetEntity(XElement xElement)
        {
            if (xElement.Descendants().Where(e => e.Name.LocalName == "binding")
                    .FirstOrDefault(x => x.Attribute("name")?.Value == "person") is XElement el
                    && el.Descendants().FirstOrDefault(e => e.Name.LocalName == "uri") is XElement uriEl
                    && !string.IsNullOrEmpty(uriEl.Value))
            {
                int id = uriEl.Value.IndexOf("Q");
                if (id > 0)
                {
                    return uriEl.Value[id..];
                }
            }
            return null;
        }
    }
}
