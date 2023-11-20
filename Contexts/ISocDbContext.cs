using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SqlObjectCopy.Models;
using System;

namespace SqlObjectCopy.Contexts
{
    public interface ISocDbContext : IDisposable
    {
        public DbSet<Table> Tables { get; set; }
        public DbSet<Routine> Routines { get; set; }
        public DbSet<Schema> Schemata { get; set; }
        public DbSet<Script> Scripts { get; set; }
        public DbSet<Column> Columns { get; set; }
        public DbSet<Constraint> Constraints { get; set; }
        public DbSet<Domain> Domains { get; set; }

        public DatabaseFacade Database { get; }
    }
}
