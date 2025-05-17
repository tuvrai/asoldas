namespace DbFill
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //PersonData? x = await WikipediaApi.GetPerson("Steve Jobs");

            var x = await WikipediaApi.GetEventsFromDay(0, 17);
            Console.ReadLine();
        }
    }
}
