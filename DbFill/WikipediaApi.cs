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
using static System.Net.Mime.MediaTypeNames;
using static System.Formats.Asn1.AsnWriter;
using System.Security.Authentication;

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
                PrepareHttpClient();
            }
            WikiDataset wikiDataset = new();
            if (month < 1 || _dayMax[month] < day)
            {
                return wikiDataset;
            }

            List<RawEvent> preprocessedEvents = await PreprocessEvents(month, day);
            List<string> allLinked = preprocessedEvents.SelectMany(x => x.LinkedArticleTitles).ToList();

            Dictionary<string, Person> persons = [];
            if (allLinked.Count > 0)
            {
                List<int> batches = [50, 20, 5];
                foreach (int batchSize in batches)
                {
                    try
                    {
                        persons = await ProcessPeople(batchSize, allLinked);
                        break;
                    }
                    catch
                    {
                        if (batchSize == batches.Last())
                        {
                            Console.WriteLine($"Could not parse people from events of {month}/{day}");
                            return wikiDataset;
                        }
                        continue;
                    }
                }

                foreach (WikiEvent wevent in preprocessedEvents.Select(x => x.GetWikiEvent(persons)))
                {
                    wikiDataset.AddEvent(wevent);
                }

                foreach (Person person in persons.Values)
                {
                    wikiDataset.AddPerson(person);
                }
            }
            else
            {
                Console.WriteLine("Error - not a single link.");
            }
            return wikiDataset;

        }

        private async static Task<Dictionary<string, Person>> ProcessPeople(int batchSize, List<string> allLinked)
        {
            Dictionary<string, Person> persons = [];
            int maxBatchId = (allLinked.Count - 1) / batchSize;
            for (int batchId = 0; batchId <= maxBatchId; batchId++)
            {
                int startId = batchId * batchSize;

                foreach (KeyValuePair<string, Person> pair in await GetPersonDict(await GetPersonToEntityDict(allLinked.Slice(startId, Math.Min(batchSize, allLinked.Count - startId)))))
                {
                    persons.Add(pair.Key, pair.Value);
                }
            }

            return persons;
        }

        private static async Task<List<RawEvent>> PreprocessEvents(int month, int day)
        {
            string urlFormat = @"https://en.wikipedia.org/w/api.php?action=query&format=json&prop=revisions&rvprop=content&titles={0}%20{1}&origin=*";
            string actualUrl = string.Format(urlFormat, _monthFull[month], day);
            string data = await GetHttpContent(actualUrl);

            int eventsStartId = data.IndexOf(events);
            int birthsStartId = data.IndexOf(births);

            Stopwatch watch = Stopwatch.StartNew();
            long lastStopElapsed = watch.ElapsedMilliseconds;

            List<RawEvent> rawEvents = [];

            if (eventsStartId >= 0 && birthsStartId >= 0)
            {
                string[] lines = data.Substring(eventsStartId, birthsStartId - eventsStartId).Split("\\n*").Where(x => x.Trim().StartsWith("[[") || x.Contains("&ndash;") || x.Contains("\\u2013")).ToArray();
                int lineCount = lines.Length;
                int currentLine = 1;
                foreach (string line in lines)
                {
                    WikiEvent newEvent = new();
                    Console.Clear();
                    Console.WriteLine($"{day}/{_monthFull[month]}, {currentLine}/{lineCount} - {line[..Math.Min(Math.Abs(line.Length - 1), 40)]}");
                    try
                    {

                        string datePart = line.Contains("&ndash;") ? line.Split("&ndash;", 2)[0] : line.Replace("\\u2013", "&ndash;").Split("&ndash;", 2)[0];
                        string eventPart = line.Contains("&ndash;") ? line.Split("&ndash;", 2)[1] : line.Replace("\\u2013", "&ndash;").Split("&ndash;", 2)[1];

                        Stop(watch, ref lastStopElapsed, "a. split");
                        if (ParseAcYear(datePart) is int year)
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

                            rawEvents.Add(new RawEvent
                            {
                                Day = newEvent.Day,
                                Description = cleanEventDescr,
                                LinkedArticleTitles = linked
                            });
                        }
                        else if (datePart.Contains("BC"))
                        {
                            // ignore BC
                        }
                        else
                        {
                            Console.WriteLine($"Year info {datePart} could not have been parsed.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);
                    }

                    currentLine++;
                }
            }

            return rawEvents;
        }

        private static void PrepareHttpClient()
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MyBot/1.0 (https://example.com)");
            _httpClient.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate");
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

        public async static Task<Dictionary<string, string>> GetPersonToEntityDict(List<string> articleNames)
        {
            var stopw = Stopwatch.StartNew();
            long curr = stopw.ElapsedMilliseconds;
            string urlFormat = @"https://en.wikipedia.org/w/api.php?action=query&titles={0}&prop=pageprops&format=xml&redirects=1";
            string articleUrl = string.Format(urlFormat, Uri.EscapeDataString(GetUrlTitleArg(articleNames)));


            string xmlContent = await GetHttpContent(articleUrl);

            if (xmlContent.Contains("toomanyvalues"))
            {
                throw new Exception("Too many values");
            }

            Stop(stopw, ref curr, $"\tarticle batch - a. http");

            XDocument doc = XDocument.Parse(xmlContent);

            Stop(stopw, ref curr, $"\ttarticle batch - a. parse");
            Dictionary<string, string> dict = [];
            foreach (XElement pageEl in doc.Descendants().Where(e => e.Name.LocalName == "page"))
            {
                if (pageEl.Attribute("title") is XAttribute titleAttr && !string.IsNullOrEmpty(titleAttr.Value) && !dict.ContainsKey(titleAttr.Value))
                {
                    if (pageEl.Descendants().FirstOrDefault(e => e.Name.LocalName == "pageprops") is XElement pageprops)
                    {
                        if (pageprops.Attribute("wikibase_item") is XAttribute entityAttr)
                        {
                            dict.Add(titleAttr.Value, entityAttr.Value);
                        }
                        else
                        {
                            Console.WriteLine($"Ignored {titleAttr.Value}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Ignored {titleAttr.Value}");
                    }
                }
                else
                {
                    Console.WriteLine("Missing title");
                }
            }
            Stop(stopw, ref curr, $"\tarticle batch - a. desc");
            return dict;
        }

        private async static Task<Dictionary<string, Person>> GetPersonDict(Dictionary<string, string> personToEntity)
        {
            string start = "https://query.wikidata.org/sparql?query=";
            string sparqla = "SELECT ?person ?name ?isHuman ?birthDate ?deathDate WHERE {VALUES ?person { REPLACABLE } OPTIONAL { ?person wdt:P31 wd:Q5. BIND(true AS ?isHuman) } OPTIONAL { ?person wdt:P569 ?birthDate. } OPTIONAL { ?person wdt:P570 ?deathDate. } OPTIONAL { ?person rdfs:label ?name. FILTER (lang(?name) = \"en\") }}";
            string query = sparqla.Replace("REPLACABLE", string.Join(' ', personToEntity.Values.Select(x => $"wd:{x}")));

            string queryUrl = start + Uri.EscapeDataString(query);

            string xmlContent = await GetHttpContent(queryUrl);

            XDocument doc = XDocument.Parse(xmlContent);

            Dictionary<string, Person> dict = [];
            foreach (XElement result in doc.Descendants().Where(e => e.Name.LocalName == "result"))
            {
                if (SparqlQueries.GetEntity(result) is string entity && personToEntity.ContainsValue(entity))
                {
                    string key = personToEntity.FirstOrDefault(pair => pair.Value == entity).Key;
                    if (!dict.ContainsKey(key) && SparqlQueries.IsHuman(result))
                    {
                        if (SparqlQueries.GetDate(result, "birthDate") is DateOnly birthDate)
                        {
                            Person person = new()
                            {
                                EntityId = entity,
                                FullName = key,
                                BirthDate = birthDate,
                                DeathDate = SparqlQueries.GetDate(result, "deathDate")
                            };
                            dict.Add(key, person);
                        }
                        else
                        {
                            Console.WriteLine($"{key} is human, incorrect birthdate.");
                        }
                    }
                }

            }

            return dict;
        }

        private static string GetUrlTitleArg(List<string> titles)
        {
            return string.Join('|', titles);
        }

        private static async Task<string> GetHttpContent(string url)
        {
            return await _httpClient.GetStringAsync(url);
        }

        private static int? ParseAcYear(string textYear)
        {
            string parsed = WikiMarkupParsers.IsLink(textYear) ? WikiMarkupParsers.ReadWikipediaLinkArticleName(textYear) : textYear.Trim();
            int? year = null;
            if (!parsed.Contains("BC"))
            {
                year = int.TryParse(parsed.Replace("AD", string.Empty).Trim(), out int res) ? res : null;
            }

            return year;
        }

    }
}
