using DataModel;
using Microsoft.EntityFrameworkCore;

namespace Asoldas.Components.Data
{
    public class WikiDbContext : DbContext
    {
        public WikiDbContext(DbContextOptions<WikiDbContext> options) : base(options) { }

        public DbSet<WikiEvent> WikiEvents { get; set; }
        public DbSet<Person> People { get; set; }
    }
}
