using DataModel;

namespace DbFill
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //PersonData? x = await WikipediaApi.GetPerson("Steve Jobs");

            WikiDataset dataset = new WikiDataset();
            for (int month = 1; month <= 2; month++)
            {
                for (int day = 1; day <= 31; day++)
                {
                    WikiDataset x = await WikipediaApi.GetEventsFromDay(month, day);
                    foreach (WikiEvent e in x.Events)
                    {
                        dataset.AddEvent(e);
                    }
                    foreach (Person p in x.People)
                    {
                        dataset.AddPerson(p);
                    }
                }
            }
            SerializeToJson(dataset).Wait();
            Console.ReadLine();

        }

        private static async Task SerializeToJson(WikiDataset wikiDataset)
        {
            System.IO.File.WriteAllText(@"genevents.json", Newtonsoft.Json.JsonConvert.SerializeObject(wikiDataset.Events, Newtonsoft.Json.Formatting.Indented));
            System.IO.File.WriteAllText(@"genpeople.json", Newtonsoft.Json.JsonConvert.SerializeObject(wikiDataset.People, Newtonsoft.Json.Formatting.Indented));

        }
    }
}
