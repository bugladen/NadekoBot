using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Services.Database
{
    public abstract class NadekoContext : DbContext
    {
        public DbSet<Quote> Quotes { get; }

        protected abstract override void OnConfiguring(DbContextOptionsBuilder optionsBuilder);
    }
}
