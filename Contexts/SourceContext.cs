using Microsoft.EntityFrameworkCore;
using SqlObjectCopy.Configuration;
using SqlObjectCopy.Models;
using System.Linq;

namespace SqlObjectCopy.Contexts
{
    public class SourceContext : DbContext, ISocDbContext
    {
        private readonly SocConfiguration _configuration;

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
        public SourceContext(SocConfiguration configuration)
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
            modelBuilder.Entity<Columns>(entity =>
            {
                entity.ToView("columns", "sys");
                entity.HasNoKey();
                entity.Property(p => p.ObjectId).HasColumnName("object_id");
                entity.Property(p => p.Name).HasColumnName("name");
                entity.Property(p => p.IsComputed).HasColumnName("is_computed");
            });
        }

        /// <summary>
        /// Configure this context - called upon first initialization of this class
        /// </summary>
        /// <param name="optionsBuilder">options builder class</param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string connectionString = _configuration.Connections.Where(c => c.Selected).First<Connection>().Source;
                if (!string.IsNullOrEmpty(connectionString))
                {
                    // use this
                    optionsBuilder.UseSqlServer(connectionString);
                }
            }
        }
    }
}
