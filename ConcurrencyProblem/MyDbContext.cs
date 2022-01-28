using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConcurrencyProblem
{
    public class MyDbContext : DbContext
    {
        public DbSet<MyEntity> MyTable { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Server = 127.0.0.1; Port = 5432; Database = myDataBase; User Id = postgres; Password = postgres;Include Error Detail=true;")
                .UseLowerCaseNamingConvention()
                .EnableDetailedErrors()
                .EnableSensitiveDataLogging();
            //optionsBuilder.LogTo(message => Debug.WriteLine(message), LogLevel.Information).EnableDetailedErrors().EnableSensitiveDataLogging();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MyEntity>()
                .HasIndex(p => new { p.ColA, p.ColB, p.MySeq }).IsUnique();

        }
    }
}
