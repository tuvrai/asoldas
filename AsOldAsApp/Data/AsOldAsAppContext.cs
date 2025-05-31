using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DataModel;

namespace AsOldAsApp.Data
{
    public class AsOldAsAppContext : DbContext
    {
        public AsOldAsAppContext (DbContextOptions<AsOldAsAppContext> options)
            : base(options)
        {
        }

        public DbSet<DataModel.WikiEvent> WikiEvent { get; set; } = default!;
        public DbSet<DataModel.Person> People { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Assuming the relationship is many-to-many
            modelBuilder.Entity<DataModel.WikiEvent>()
                .HasMany(e => e.People)
                .WithMany(); // You can also specify inverse nav property if you have one
        }
    }
}
