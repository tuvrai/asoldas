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
using System.Data;
using System.Net.Http.Headers;
using System.Diagnostics;
using System.Net;

namespace DbFill
{
    
    public static class WikipediaApi
    {
        private readonly static string DeathPropertyId = "P570";
        private readonly static string BirthPropertyId = "P569";
        private readonly static string[] _monthFull = [string.Empty, "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
        private readonly static int[] _dayMax = [0,31,29,31,30,31,30,31,31,30,31,30,31];


        private static readonly string events = "==Events==";
        private static readonly string births = "==Births==";

        private static readonly int ForcedBreakTimeMs = 50;

        private static HttpClient _httpClient = null;

        public async static Task<WikiDataset> GetEventsFromDay(int month, int day)
        {
            if (_httpClient == null)
            {
                HttpClientHandler handler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };
                _httpClient = new HttpClient(handler);
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MyBot/1.0 (https://example.com)");
                _httpClient.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate");
            }
            WikiDataset wikiDataset = new();
            if (month < 1 || _dayMax[month] < day)
            {
                return wikiDataset;
            }

            string urlFormat = @"https://en.wikipedia.org/w/api.php?action=query&format=json&prop=revisions&rvprop=content&titles={0}%20{1}&origin=*";
            string actualUrl = string.Format(urlFormat, _monthFull[month], day);
            string data = await GetHttpContent(actualUrl);

            int eventsStartId = data.IndexOf(events);
            int birthsStartId = data.IndexOf(births);

            Stopwatch watch = Stopwatch.StartNew();
            long lastStopElapsed = watch.ElapsedMilliseconds;
            if (eventsStartId >= 0 && birthsStartId >= 0)
            {
                string[] lines = data.Substring(eventsStartId, birthsStartId - eventsStartId).Split("\\n*").Where(x => x.Trim().StartsWith("[[")).ToArray();
                int lineCount = lines.Length;
                int currentLine = 1;
                foreach (string line in lines)
                {
                    WikiEvent newEvent = new();
                    Console.Clear();
                    Console.WriteLine($"{day}/{_monthFull[month]}, {currentLine}/{lineCount} - {line[..40]}");
                    try
                    {
                        
                        string datePart = line.Contains("&ndash;") ? line.Split("&ndash;", 2)[0] : line.Replace("\\u2013", "&ndash;").Split("&ndash;", 2)[0];
                        string eventPart = line.Contains("&ndash;") ? line.Split("&ndash;", 2)[1] : line.Replace("\\u2013", "&ndash;").Split("&ndash;", 2)[1];

                        Stop(watch, ref lastStopElapsed, "a. split");
                        if (ParseAcYear(WikiMarkupParsers.ReadWikipediaLinkArticleName(datePart)) is int year)
                        {
                            Stop(watch, ref lastStopElapsed, "a. parseacyear");
                            newEvent.Day = new DateOnly(year, month, day);
                            string cleanEventDescr = eventPart
                                .RemoveRefs()
                                .RemoveExcessNewLines()
                                .FlattenLinks(out List<string> linked)
                                .ConvertUtfCharacters()
                                .Trim();
                            Stop(watch, ref lastStopElapsed, "a. cleaneventdescr");
                            newEvent.Description = cleanEventDescr;

                            foreach (string name in linked)
                            {
                                if (await GetWikiItemId(name) is string entity)
                                {
                                    Stop(watch, ref lastStopElapsed, "a. GetWikiItemId (OK)");
                                    if (await SparqlQueries.GetPersonUsingEntityId(entity) is Person person)
                                    {
                                        Stop(watch, ref lastStopElapsed, "a. GetPersonUsingEntityId (OK)");
                                        person.FullName = name;
                                        if (newEvent.AddPerson(person))
                                        {
                                            person.Events.Add(newEvent);
                                            wikiDataset.AddPerson(person);
                                        }
                                    }
                                    else
                                    {
                                        Stop(watch, ref lastStopElapsed, "a. GetPersonUsingEntityId (NOK)");
                                    }
                                }
                                else
                                {
                                    Stop(watch, ref lastStopElapsed, "a. getwikiitemid (NOK)");
                                }
                            }

                            wikiDataset.AddEvent(newEvent);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);
                    }

                    currentLine++;
                    Thread.Sleep(ForcedBreakTimeMs);
                }
            }
            return wikiDataset;
        }

        private static void Stop(this Stopwatch stopwatch, ref long lastMs, string comment)
        {
            Console.WriteLine($"{comment}: {stopwatch.ElapsedMilliseconds - lastMs}");
            lastMs = stopwatch.ElapsedMilliseconds;
        }

        public static string RemoveExcessNewLines(this string text)
        {
            return text.Split("\\n")[0];
        }

        public static string RemoveExcessVariables(this string text)
        {
            return text.Split("\\n")[0];
        }

        public static string ConvertUtfCharacters(this string text)
        {
            string output = Regex.Replace(text, @"\\u([0-9A-Fa-f]{4})", match =>
            {
                string hex = match.Groups[1].Value;
                int code = Convert.ToInt32(hex, 16);
                return ((char)code).ToString();
            });

            return output;
        }
        

        public async static Task<string?> GetWikiItemId(string articleName)
        {
            var stopw = Stopwatch.StartNew();
            long curr = stopw.ElapsedMilliseconds;
            string urlFormat = @"https://en.wikipedia.org/w/api.php?action=query&titles={0}&prop=pageprops&format=xml";
            string articleUrl = string.Format(urlFormat,Uri.EscapeDataString(articleName));


            string xmlContent = await GetHttpContent(articleUrl);

            Stop(stopw, ref curr, $"\t{articleName} - a. http");

            XDocument doc = XDocument.Parse(xmlContent);

            Stop(stopw, ref curr, $"\t{articleName} - a. parse");

            if (doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "pageprops") is XElement pageprops)
            {
                if (pageprops.Attribute("wikibase_item") is XAttribute wikibaseAttr)
                {
                    return wikibaseAttr.Value;
                }    
            }
            Stop(stopw, ref curr, $"\t{articleName} - a. desc");
            return null;
        }

        private static async Task<string> GetHttpContent(string url)
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MyBot/1.0 (https://example.com)");
            _httpClient.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate");


            return await _httpClient.GetStringAsync(url);
        }

        private static int? ParseAcYear(string date)
        {
            int? year = null;
            if (!date.Contains("BC"))
            {
                year = int.TryParse(date.Replace("AD", string.Empty).Trim(), out int res) ? res : null;
            }

            return year;
        }

    }
}
