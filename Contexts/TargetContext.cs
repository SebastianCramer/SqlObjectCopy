using Microsoft.EntityFrameworkCore;
using SqlObjectCopy.Configuration;
using SqlObjectCopy.Models;
using System.Linq;

namespace SqlObjectCopy.Contexts
{
    public class TargetContext : DbContext, ISocDbContext
    {
        private readonly SocConfiguration _configuration;

        #region Models

        public DbSet<Table> Tables { get; set; }
        public DbSet<Routine> Routines { get; set; }
        public DbSet<Column> Columns { get; set; }

        public DbSet<Schema> Schemata { get; set; }
        public DbSet<Script> Scripts { get; set; }
        public DbSet<Constraint> Constraints { get; set; }
        public DbSet<Domain> Domains { get; set; }
        public DbSet<Type> Types { get; set; }

        #endregion

        /// <summary>
        /// Constructuor
        /// </summary>
        /// <param name="configuration">Configuration for DB Connection retrieval</param>
        /// <param name="logger">Logger instance</param>
        public TargetContext(SocConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Table>().ToView("TABLES", "INFORMATION_SCHEMA").HasNoKey();
            modelBuilder.Entity<Routine>().ToView("ROUTINES", "INFORMATION_SCHEMA").HasNoKey();
            modelBuilder.Entity<Schema>().ToView("SCHEMATA", "INFORMATION_SCHEMA").HasNoKey();
            modelBuilder.Entity<Constraint>().ToView("REFERENTIAL_CONSTRAINTS", "INFORMATION_SCHEMA").HasNoKey();
            modelBuilder.Entity<Domain>().ToView("DOMAINS", "INFORMATION_SCHEMA").HasNoKey();
            modelBuilder.Entity<Script>().HasNoKey();
            modelBuilder.Entity<Type>(entity =>
            {
                entity.ToView("types", "sys");
                entity.Property(p => p.SystemTypeId).HasColumnName("system_type_id");
                entity.Property(p => p.Name).HasColumnName("name");
                entity.HasNoKey();
            });
            modelBuilder.Entity<Column>(entity =>
            {
                entity.ToView("columns", "sys");
                entity.HasNoKey();
                entity.Property(p => p.ObjectId).HasColumnName("object_id");
                entity.Property(p => p.Name).HasColumnName("name");
                entity.Property(p => p.IsComputed).HasColumnName("is_computed");
                entity.Property(p => p.SystemTypeId).HasColumnName("system_type_id");
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
                string connectionString = _configuration.Connections.Where(c => c.Selected).First<Connection>().Target;
                if (!string.IsNullOrEmpty(connectionString))
                {
                    // use this
                    optionsBuilder.UseSqlServer(connectionString);
                }
            }
        }
    }
}
