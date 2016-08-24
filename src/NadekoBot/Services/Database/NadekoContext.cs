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
        public DbSet<Quote> Quotes { get; set; }
        public DbSet<Donator> Donators { get; set; }
        public DbSet<Config> Configs { get; set; }

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
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=./data/NadekoBot.sqlite");
        }
    }
}
