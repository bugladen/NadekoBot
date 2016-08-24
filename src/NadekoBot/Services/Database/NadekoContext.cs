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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region QUOTES
            
            var quoteEntity = modelBuilder.Entity<Quote>();

            #endregion


            #region Donators

            var donatorEntity = modelBuilder.Entity<Donator>();
            donatorEntity
                .HasIndex(d => d.UserId)
                .IsUnique();

            #endregion

            #region Config

            var configEntity = modelBuilder.Entity<Config>();
            configEntity
                .HasIndex(c => c.GuildId)
                .IsUnique();

            #endregion
        }
        protected abstract override void OnConfiguring(DbContextOptionsBuilder optionsBuilder);
    }
}
