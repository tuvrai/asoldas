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
        private static string getPersonQueryFormat = @"https://query.wikidata.org/sparql?query=SELECT%20%3Fperson%20%3FpersonLabel%20%3FbirthDate%20%3FdeathDate%20WHERE%20%7B%0A%20%20%3Fperson%20rdfs%3Alabel%20%22{0}%22%40en.%0A%20%20%3Fperson%20wdt%3AP31%20wd%3AQ5.%20%20%23%20Ensure%20it%27s%20a%20human%20(person)%0A%0A%20%20OPTIONAL%20%7B%20%3Fperson%20wdt%3AP569%20%3FbirthDate.%20%7D%0A%20%20OPTIONAL%20%7B%20%3Fperson%20wdt%3AP570%20%3FdeathDate.%20%7D%0A%0A%20%20SERVICE%20wikibase%3Alabel%20%7B%20bd%3AserviceParam%20wikibase%3Alanguage%20%22en%22.%20%7D%0A%7D";

        internal static async Task<PersonData?> GetPersonUsingFullName(string fullName)
        {
            string urlName = Uri.EscapeDataString(fullName);
            string namequery = string.Format(getPersonQueryFormat, urlName);

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MyCSharpApp/1.0 (example@example.com)");


            string xmlContent = await httpClient.GetStringAsync(namequery);

            XDocument doc = XDocument.Parse(xmlContent);

            PersonData? personData = null;

            if (doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "result") is XElement result)
            {
                personData = new PersonData
                {
                    FullName = fullName,
                };
                foreach (XElement element in result.Descendants().Where(e => e.Name.LocalName == "binding"))
                {
                    if (element.Attribute("name").Value == "birthDate")
                    {
                        string[] arr = element.Value.Split('T')[0].Split('-');
                        personData.BirthDate = new DateTime(int.Parse(arr[0]), int.Parse(arr[1]), int.Parse(arr[2]));
                    }
                    if (element.Attribute("name").Value == "deathDate")
                    {
                        string[] arr = element.Value.Split('T')[0].Split('-');
                        personData.DeathDate = new DateTime(int.Parse(arr[0]), int.Parse(arr[1]), int.Parse(arr[2]));
                    }
                }
            }

            return personData;
        }

        internal static async Task<PersonData> GetPersonUsingEntityId(string entityId)
        {
            string queryFormat = @"https://query.wikidata.org/sparql?query=SELECT%20%3FisHuman%20%3FbirthDate%20%3FdeathDate%20WHERE%20%7B%0A%20%20OPTIONAL%20%7B%20wd%3A{0}%20wdt%3AP31%20wd%3AQ5.%20BIND(true%20AS%20%3FisHuman)%20%7D%0A%20%20OPTIONAL%20%7B%20wd%3A{0}%20wdt%3AP569%20%3FbirthDate.%20%7D%0A%20%20OPTIONAL%20%7B%20wd%3A{0}%20wdt%3AP570%20%3FdeathDate.%20%7D%0A%7D%0A";
            string actualQuery = string.Format(queryFormat, entityId);

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MyCSharpApp/1.0 (example@example.com)");

            string xmlContent = await httpClient.GetStringAsync(actualQuery);

            XDocument doc = XDocument.Parse(xmlContent);

            PersonData? personData = null;

            if (doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "result") is XElement result)
            {
                personData = new PersonData()
                {
                    EntityId = entityId,
                };
                foreach (XElement element in result.Descendants().Where(e => e.Name.LocalName == "binding"))
                {
                    if (element.Attribute("name").Value == "birthDate")
                    {
                        string[] arr = element.Value.Split('T')[0].Split('-');
                        try
                        {
                            personData.BirthDate = new DateTime(int.Parse(arr[0]), int.Parse(arr[1]), int.Parse(arr[2]));
                        }
                        catch
                        {
                            Console.WriteLine($"\t\tError: weird date - {element.Value}");
                            personData.BirthDate = null;
                        }
                    }
                    if (element.Attribute("name").Value == "deathDate")
                    {
                        string[] arr = element.Value.Split('T')[0].Split('-');
                        try
                        {
                            personData.DeathDate = new DateTime(int.Parse(arr[0]), int.Parse(arr[1]), int.Parse(arr[2]));
                        }
                        catch
                        {
                            Console.WriteLine($"\t\tError: weird date - {element.Value}");
                            personData.DeathDate = null;
                        }
                    }
                }
            }
            if (personData?.BirthDate != null)
            {
                return personData;
            }
            else
            {
                return null;
            }
        }
    }
}
