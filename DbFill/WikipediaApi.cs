using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using static System.Net.WebRequestMethods;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace DbFill
{
    public class PersonData
    {
        public string FullName { get; set; }
        public string EntityId { get; set; }
        public DateTime? BirthDate { get; set; }
        public DateTime? DeathDate { get; set; }
    }
    public static class WikipediaApi
    {
        private static string DeathPropertyId = "P570";
        private static string BirthPropertyId = "P569";
        private static string[] _monthFull = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
        private static int[] _dayMax = [31,29,31,30,31,30,31,31,30,31,30,31];

        private static string getPersonQueryFormat = @"https://query.wikidata.org/sparql?query=SELECT%20%3Fperson%20%3FpersonLabel%20%3FbirthDate%20%3FdeathDate%20WHERE%20%7B%0A%20%20%3Fperson%20rdfs%3Alabel%20%22{0}%22%40en.%0A%20%20%3Fperson%20wdt%3AP31%20wd%3AQ5.%20%20%23%20Ensure%20it%27s%20a%20human%20(person)%0A%0A%20%20OPTIONAL%20%7B%20%3Fperson%20wdt%3AP569%20%3FbirthDate.%20%7D%0A%20%20OPTIONAL%20%7B%20%3Fperson%20wdt%3AP570%20%3FdeathDate.%20%7D%0A%0A%20%20SERVICE%20wikibase%3Alabel%20%7B%20bd%3AserviceParam%20wikibase%3Alanguage%20%22en%22.%20%7D%0A%7D";

        private static string events = "==Events==";
        private static string births = "==Births==";

        public async static Task<string> GetEventsFromDay(int month, int day)
        {
            string urlFormat = @"https://en.wikipedia.org/w/api.php?action=query&format=json&prop=revisions&rvprop=content&titles={0}%20{1}&origin=*";
            string actualUrl = string.Format(urlFormat, _monthFull[month], day);

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MyCSharpApp /1.0 (example@example.com)");

            string data = await httpClient.GetStringAsync(actualUrl);

            int start = data.IndexOf(events);
            int end = data.IndexOf(births);

            if (start >= 0 && end >= 0)
            {
                string[] lines = data.Substring(start, end - start).Split("\\n*").Where(x => x.Trim().StartsWith("[[")).ToArray();

                foreach (string line in lines)
                {
                    string datePart = line.Split("&ndash;")[0];
                    string eventPart = line.Split("&ndash;")[1];

                    string date = ReadWikipediaLinkArticleName(datePart);

                    int dateN = 0;
                    if (date.Contains("BC"))
                    {
                        dateN = int.TryParse(date.Replace("BC", string.Empty).Trim(), out int res) ? res : 0;
                        dateN *= (-1);
                    }
                    else
                    {
                        dateN = int.TryParse(date.Trim(), out int res) ? res : 0;
                    }

                    if (dateN != 0)
                    {
                        string flatten = eventPart.RemoveRefs().RemoveExcessNewLines().FlattenLinks(out List<string> linked).Trim();

                        string output = Regex.Replace(flatten, @"\\u([0-9A-Fa-f]{4})", match =>
                        {
                            string hex = match.Groups[1].Value;
                            int code = Convert.ToInt32(hex, 16);
                            return ((char)code).ToString();
                        });

                        foreach (string name in linked)
                        {
                            Console.WriteLine(name);
                            if (await GetWikiItemId(name) is string entity)
                            {
                                Console.WriteLine("\t"+entity);
                                if (await GetPersonData(entity) is PersonData personData)
                                {
                                    Console.WriteLine($"{name}, born: {personData.BirthDate}, dead: {personData.DeathDate}");
                                }
                            }
                        }
                        Console.WriteLine($"{dateN} - {output}");
                    }
                    else
                    {
                        Console.WriteLine("ERROR");
                    }

                }
            }

            return string.Empty;
        }

        //public static string ParseWikiText(string rawText)
        //{

        //}

        public static string RemoveRefs(this string text)
        {
            string pattern = @"<ref(.*)<\/ref>";
            string newText = text;
            MatchCollection matches = Regex.Matches(text, pattern);

            Match match = Regex.Match(newText, pattern);
            while (match.Success)
            {
                newText = newText.Substring(0, match.Index) + newText.Substring(match.Index + match.Length);

                match = Regex.Match(newText, pattern);
            }

            return newText;
        }

        public static string RemoveExcessNewLines(this string text)
        {
            return text.Split("\\n")[0];
        }

        public static string RemoveExcessVariables(this string text)
        {
            return text.Split("\\n")[0];
        }

        public static string FlattenLinks(this string text, out List<string> linkedPages)
        {
            string pattern = @"\[\[(.*?)\]\]";
            string newText = text;
            MatchCollection matches = Regex.Matches(text, pattern);

            linkedPages = [];

            Match match = Regex.Match(newText, pattern);
            while (match.Success)
            {
                string parsedValue = ReadWikipediaLinkDisplayText(match.Value);
                linkedPages.Add(ReadWikipediaLinkArticleName(match.Value));
                newText = newText.Substring(0, match.Index) + parsedValue + newText.Substring(match.Index + match.Length);

                match = Regex.Match(newText, pattern);
            }

            return newText;

            //foreach (Match match in matches)
            //{
            //    string parsedValue = ReadWikipediaLinkDisplayText(match.Value);
            //    linkedPages.Add(ReadWikipediaLinkArticleName(match.Value));
            //    text = text.Substring(0, match.Index) + parsedValue + text.Substring(match.Index + match.Length);
            //    Console.WriteLine("Match: " + match.Value);
            //    Console.WriteLine("Content: " + match.Groups[1].Value);
            //    Console.WriteLine("Start Index: " + match.Index);
            //    Console.WriteLine("End Index: " + (match.Index + match.Length - 1));
            //    Console.WriteLine();
            //}

        }

        public static string ReadWikipediaLinkArticleName(string link)
        {
            string inner = link.Replace("[[", string.Empty).Replace("]]", string.Empty);
            
            if (inner.Contains("|"))
            {
                inner = inner.Split("|")[0];
            }

            return inner;
        }

        public static string ReadWikipediaLinkDisplayText(string link)
        {
            string inner = link.Replace("[[", string.Empty).Replace("]]", string.Empty);

            if (inner.Contains("|"))
            {
                inner = inner.Split("|")[1];
            }

            return inner;
        }

        public async static Task<string> GetWikiItemId(string articleName)
        {
            string urlFormat = @"https://en.wikipedia.org/w/api.php?action=query&titles={0}&prop=pageprops&format=xml";
            string urlName = string.Format(urlFormat,Uri.EscapeDataString(articleName));

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MyCSharpApp/1.0 (example@example.com)");

            string xmlContent = await httpClient.GetStringAsync(urlName);

            XDocument doc = XDocument.Parse(xmlContent);

            if (doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "pageprops") is XElement result)
            {
                if (result.Attribute("wikibase_item") != null)
                {
                    return result.Attribute("wikibase_item").Value;
                }    
            }

            return null;
        }

        public static async Task<PersonData> GetPersonData(string id)
        {
            string queryFormat = @"https://query.wikidata.org/sparql?query=SELECT%20%3FisHuman%20%3FbirthDate%20%3FdeathDate%20WHERE%20%7B%0A%20%20OPTIONAL%20%7B%20wd%3A{0}%20wdt%3AP31%20wd%3AQ5.%20BIND(true%20AS%20%3FisHuman)%20%7D%0A%20%20OPTIONAL%20%7B%20wd%3A{0}%20wdt%3AP569%20%3FbirthDate.%20%7D%0A%20%20OPTIONAL%20%7B%20wd%3A{0}%20wdt%3AP570%20%3FdeathDate.%20%7D%0A%7D%0A";
            string actualQuery = string.Format(queryFormat, id);

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MyCSharpApp/1.0 (example@example.com)");

            string xmlContent = await httpClient.GetStringAsync(actualQuery);

            XDocument doc = XDocument.Parse(xmlContent);

            PersonData? personData = null;

            if (doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "result") is XElement result)
            {
                personData = new PersonData();
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

        public static async Task<PersonData?> GetPerson(string fullName)
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
    }
}
