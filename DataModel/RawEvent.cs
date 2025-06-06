﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModel
{
    public class RawEvent
    {
        //public string Id { get; set; } = string.Empty;
        public DateOnly Day { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> LinkedArticleTitles { get; set; } = [];

        public WikiEvent GetWikiEvent(Dictionary<string, Person> articlePersonDict)
        {
            WikiEvent wikiEvent = new WikiEvent()
            {
                Day = this.Day,
                Description = this.Description,
            };
            foreach (Person person in articlePersonDict.Where(x => LinkedArticleTitles.Any(l => l.Equals(x.Key, StringComparison.OrdinalIgnoreCase))).Select(x => x.Value))
            {
                wikiEvent.AddPerson(person);
            }
            return wikiEvent;
        }
    }
}
