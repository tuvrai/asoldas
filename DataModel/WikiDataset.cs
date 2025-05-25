using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModel
{
    public class WikiDataset
    {
        public List<WikiEvent> Events { get; set; } = [];
        public List<Person> People { get; set; } = [];

        public void SetUniqueId(WikiEvent ev)
        {
            int count = 1;
            string hash = string.Empty;
            do
            {
                hash = WikiEvent.GenerateId(ev.Day, count);
                count++;
            } while (!Events.All(x => x.Id != hash));
            ev.Id = hash;
        }

        public void AddEvent(WikiEvent ev)
        {
            SetUniqueId(ev);
            Events.Add(ev);
        }

        public void AddPerson(Person person)
        {
            if (!string.IsNullOrEmpty(person.EntityId) && !People.Any(x => x.EntityId == person.EntityId))
            {
                People.Add(person);
            }
        }
    }
}
