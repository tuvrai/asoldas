using DataModel;

namespace DbFill
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //PersonData? x = await WikipediaApi.GetPerson("Steve Jobs");

            WikiDataset dataset = new WikiDataset();
            for (int month = 1; month <= 1; month++)
            {
                for (int day = 1; day <= 7; day++)
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
            System.IO.File.WriteAllText(@"W:\projekty\asoldas\Asoldas\DataModel\17_01_events.json", Newtonsoft.Json.JsonConvert.SerializeObject(wikiDataset.Events, Newtonsoft.Json.Formatting.Indented));
            System.IO.File.WriteAllText(@"W:\projekty\asoldas\Asoldas\DataModel\17_01_people.json", Newtonsoft.Json.JsonConvert.SerializeObject(wikiDataset.People, Newtonsoft.Json.Formatting.Indented));

        }
    }
}
