using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SqlObjectCopy.Models;
using System;

namespace SqlObjectCopy.Contexts
{
    public interface ISocDbContext : IDisposable
    {
        public DbSet<Tables> Tables { get; set; }
        public DbSet<Routines> Routines { get; set; }
        public DbSet<Schemata> Schemata { get; set; }
        public DbSet<Scripts> Scripts { get; set; }
        public DbSet<Columns> Columns { get; set; }
        public DbSet<Constraints> Constraints { get; set; }
        public DbSet<Domains> Domains { get; set; }

        public DatabaseFacade Database { get; }
    }
}
