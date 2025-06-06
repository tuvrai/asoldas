﻿using DataModel;
using Newtonsoft.Json;

namespace AsOldAsApp.Model
{
    public class DbMigrate
    {
        public List<WikiEvent> WikiEvents { get; set; } = [];
        public List<Person> People { get; set; } = [];

        public void ReadEvents()
        {
            string json = File.ReadAllText("Components/EventsData/fullevents.json");
            WikiEvents = JsonConvert.DeserializeObject<List<WikiEvent>>(json) ?? [];
        }

        public void ReadPeople()
        {
            string json = File.ReadAllText("Components/EventsData/fullpeople.json");
            People = JsonConvert.DeserializeObject<List<Person>>(json) ?? [];
        }
    }
}
