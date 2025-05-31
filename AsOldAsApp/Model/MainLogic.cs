using DataModel;
using System.Collections.Generic;

namespace AsOldAsApp.Model
{
    public class MainLogic(List<WikiEvent> wikiEvents)
    {
        List<WikiEvent> _wikiEvents = wikiEvents;

        public List<WikiEvent> GetPeopleAchievementsAtYourAge(DateOnly userBirthDate)
        {
            DateTime now = DateTime.Now;
            int usersAgeInDays = new DateOnly(now.Year, now.Month, now.Day).DayNumber - userBirthDate.DayNumber;

            List<WikiEvent> eventsWithPeopleAtUserAge = [];

            foreach (WikiEvent wikiEvent in _wikiEvents.Where(x => x.HasPeopleOfAge(usersAgeInDays)))
            {
                eventsWithPeopleAtUserAge.Add(wikiEvent.CopyWithPeopleOfAge(usersAgeInDays));
            }

            return eventsWithPeopleAtUserAge;
        }
    }
}
