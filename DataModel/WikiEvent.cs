using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModel
{
    public class WikiEvent
    {
        public DateOnly Day { get; set; }
        public string Description { get; set; }
        public List<string> InvolvedEntities { get; set; }
    }
}
