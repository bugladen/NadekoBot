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
        public DbSet<GuildConfig> GuildConfigs { get; set; }
        public DbSet<ClashWar> ClashOfClans { get; set; }
        public DbSet<ClashCaller> ClashCallers { get; set; }
        public DbSet<Reminder> Reminders { get; set; }
        public DbSet<SelfAssignedRole> SelfAssignableRoles { get; set; }
        public DbSet<BotConfig> BotConfig { get; set; }
        public DbSet<Repeater> Repeaters { get; set; }
        public DbSet<Currency> Currency { get; set; }
        public DbSet<ConvertUnit> ConversionUnits { get; set; }

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

            var configEntity = modelBuilder.Entity<GuildConfig>();
            configEntity
                .HasIndex(c => c.GuildId)
                .IsUnique();

            #endregion

            #region ClashOfClans

            var callersEntity = modelBuilder.Entity<ClashCaller>();
            callersEntity
                .HasOne(c => c.ClashWar)
                .WithMany(c => c.Bases);

            #endregion

            #region Self Assignable Roles

            var selfassignableRolesEntity = modelBuilder.Entity<SelfAssignedRole>();

            selfassignableRolesEntity
                .HasIndex(s => new { s.GuildId, s.RoleId })
                .IsUnique();

            #endregion

            #region Repeater

            var repeaterEntity = modelBuilder.Entity<Repeater>();

            repeaterEntity
                .HasIndex(r => r.ChannelId)
                .IsUnique();

            #endregion

            #region Currency
            var currencyEntity = modelBuilder.Entity<Currency>();

            currencyEntity
                .HasIndex(c => c.UserId)
                .IsUnique();
            #endregion
            
        }
        protected abstract override void OnConfiguring(DbContextOptionsBuilder optionsBuilder);
    }
}
