using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SqlObjectCopy.Models;
using System;

namespace SqlObjectCopy.Contexts
{
    public class SourceContext : DbContext, ISocDbContext
    {
        private readonly IConfiguration _configuration;

        #region Models

        public DbSet<Tables> Tables { get; set; }
        public DbSet<Routines> Routines { get; set; }
        public DbSet<Columns> Columns { get; set; }

        public DbSet<Schemata> Schemata { get; set; }
        public DbSet<Scripts> Scripts { get; set; }
        public DbSet<Constraints> Constraints { get; set; }
        public DbSet<Domains> Domains { get; set; }

        #endregion

        /// <summary>
        /// Constructuor
        /// </summary>
        /// <param name="configuration">Configuration for DB Connection retrieval</param>
        /// <param name="logger">Logger instance</param>
        public SourceContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tables>().ToView("TABLES", "INFORMATION_SCHEMA").HasNoKey();
            modelBuilder.Entity<Routines>().ToView("ROUTINES", "INFORMATION_SCHEMA").HasNoKey();
            modelBuilder.Entity<Schemata>().ToView("SCHEMATA", "INFORMATION_SCHEMA").HasNoKey();
            modelBuilder.Entity<Constraints>().ToView("REFERENTIAL_CONSTRAINTS", "INFORMATION_SCHEMA").HasNoKey();
            modelBuilder.Entity<Domains>().ToView("DOMAINS", "INFORMATION_SCHEMA").HasNoKey();
            modelBuilder.Entity<Scripts>().HasNoKey();
            modelBuilder.Entity<Columns>().ToView("columns", "sys").HasNoKey();
        }

        /// <summary>
        /// Configure this context - called upon first initialization of this class
        /// </summary>
        /// <param name="optionsBuilder">options builder class</param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string connectionString = _configuration.GetSection("Connections:Source").Value;
                if (!string.IsNullOrEmpty(connectionString))
                {
                    // use this
                    optionsBuilder.UseSqlServer(connectionString);
                }
            }
        }
    }
}
