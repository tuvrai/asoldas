
using AsOldAsApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AsOldAsApp.Model
{
    public class SeedData
    {
        internal static void Reinitialize(IServiceProvider serviceProvider)
        {
            using var context = new AsOldAsAppContext(
            serviceProvider.GetRequiredService<
                DbContextOptions<AsOldAsAppContext>>());


            if (context == null || context.WikiEvent == null)
            {
                throw new NullReferenceException(
                    "Null BlazorWebAppMoviesContext or Movie DbSet");
            }

            
            DbMigrate dbMigrate = new();
            dbMigrate.ReadEvents();
            dbMigrate.ReadPeople();
            context.WikiEvent.RemoveRange(context.WikiEvent);
            context.People.RemoveRange(context.People);
            context.SaveChanges();
            context.People.AddRange(dbMigrate.People);
            PopulateContext(context, dbMigrate.WikiEvents);

            context.SaveChanges();

        }

        private static void PopulateContext(AsOldAsAppContext context, List<DataModel.WikiEvent> events)
        {
            foreach (DataModel.WikiEvent ev in events)
            {
                for (int i = 0; i < ev.People.Count; i++)
                {
                    var person = ev.People[i];

                    // If person has an existing ID, attach it instead of re-adding
                    if (context.People.Local.All(p => p.EntityId != person.EntityId))
                    {
                        context.Attach(person); // Mark as existing
                    }
                    else
                    {
                        // Reuse already tracked instance
                        ev.People[i] = context.People.Local.First(p => p.EntityId == person.EntityId);
                    }
                }
            }
            context.WikiEvent.AddRange(events);
        }
    }
}
