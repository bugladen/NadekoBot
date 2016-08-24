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
            //// guildid and keyword are unique pair
            var quoteEntity = modelBuilder.Entity<Quote>();
            //quoteEntity
            //    .HasAlternateKey(q => q.GuildId)
            //    .HasName("AK_GuildId_Keyword");

            //quoteEntity
            //    .HasAlternateKey(q => q.Keyword)
            //    .HasName("AK_GuildId_Keyword");

            quoteEntity
                .HasIndex(q => new { q.GuildId, q.Keyword })
                .IsUnique();
            

            #endregion

            #region 
            #endregion
        }
        protected abstract override void OnConfiguring(DbContextOptionsBuilder optionsBuilder);
    }
}
