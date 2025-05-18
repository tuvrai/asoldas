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
using DataModel;

namespace DbFill
{
    
    public static class WikipediaApi
    {
        private static string DeathPropertyId = "P570";
        private static string BirthPropertyId = "P569";
        private static string[] _monthFull = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
        private static int[] _dayMax = [31,29,31,30,31,30,31,31,30,31,30,31];


        private static readonly string events = "==Events==";
        private static readonly string births = "==Births==";

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

                    string date = WikiMarkupParsers.ReadWikipediaLinkArticleName(datePart);

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
                                if (await SparqlQueries.GetPersonUsingEntityId(entity) is PersonData personData)
                                {
                                    personData.FullName = name;
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

        public static string RemoveExcessNewLines(this string text)
        {
            return text.Split("\\n")[0];
        }

        public static string RemoveExcessVariables(this string text)
        {
            return text.Split("\\n")[0];
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
       
    }
}
