using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using NadekoBot.Services.Database.Models;
using NadekoBot.Extensions;

namespace NadekoBot.Services.Database
{
    public class NadekoContext : DbContext
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
        public DbSet<MusicPlaylist> MusicPlaylists { get; set; }
        public DbSet<CustomReaction> CustomReactions { get; set; }
        public DbSet<CurrencyTransaction> CurrencyTransactions { get; set; }
        public DbSet<UserPokeTypes> PokeGame { get; set; }

        //logging
        public DbSet<LogSetting> LogSettings { get; set; }
        public DbSet<IgnoredLogChannel> IgnoredLogChannels { get; set; }
        public DbSet<IgnoredVoicePresenceChannel> IgnoredVoicePresenceCHannels { get; set; }

        //orphans xD
        public DbSet<EightBallResponse> EightBallResponses { get; set; }
        public DbSet<RaceAnimal> RaceAnimals { get; set; }
        public DbSet<ModulePrefix> ModulePrefixes { get; set; }

        public NadekoContext()
        {
            this.Database.Migrate();
        }

        public NadekoContext(DbContextOptions options) : base(options)
        {
            this.Database.Migrate();
            EnsureSeedData();
        }
        ////Uncomment this to db initialisation with dotnet ef migration add [module]
        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    optionsBuilder.UseSqlite("Filename=./data/NadekoBot.db");
        //}

        public void EnsureSeedData()
        {
            if (!BotConfig.Any())
            {
                var bc = new BotConfig();

                bc.ModulePrefixes.AddRange(new HashSet<ModulePrefix>()
                {
                    new ModulePrefix() { ModuleName = "Administration", Prefix = "." },
                    new ModulePrefix() { ModuleName = "Searches", Prefix = "~" },
                    new ModulePrefix() { ModuleName = "Translator", Prefix = "~" },
                    new ModulePrefix() { ModuleName = "NSFW", Prefix = "~" },
                    new ModulePrefix() { ModuleName = "ClashOfClans", Prefix = "," },
                    new ModulePrefix() { ModuleName = "Help", Prefix = "-" },
                    new ModulePrefix() { ModuleName = "Music", Prefix = "!!" },
                    new ModulePrefix() { ModuleName = "Trello", Prefix = "trello" },
                    new ModulePrefix() { ModuleName = "Games", Prefix = ">" },
                    new ModulePrefix() { ModuleName = "Gambling", Prefix = "$" },
                    new ModulePrefix() { ModuleName = "Permissions", Prefix = ";" },
                    new ModulePrefix() { ModuleName = "Pokemon", Prefix = ">" },
                    new ModulePrefix() { ModuleName = "Utility", Prefix = "." },
                    new ModulePrefix() { ModuleName = "CustomReactions", Prefix = "." },
                    new ModulePrefix() { ModuleName = "PokeGame", Prefix = ">" }
                });
                bc.RaceAnimals.AddRange(new HashSet<RaceAnimal>
                {
                    new RaceAnimal { Icon = "🐼", Name = "Panda" },
                    new RaceAnimal { Icon = "🐻", Name = "Bear" },
                    new RaceAnimal { Icon = "🐧", Name = "Pengu" },
                    new RaceAnimal { Icon = "🐨", Name = "Koala" },
                    new RaceAnimal { Icon = "🐬", Name = "Dolphin" },
                    new RaceAnimal { Icon = "🐞", Name = "Ladybird" },
                    new RaceAnimal { Icon = "🦀", Name = "Crab" },
                    new RaceAnimal { Icon = "🦄", Name = "Unicorn" }
                });
                bc.EightBallResponses.AddRange(new HashSet<EightBallResponse>
                {
                    new EightBallResponse() { Text = "Most definitely yes" },
                    new EightBallResponse() { Text = "For sure" },
                    new EightBallResponse() { Text = "Totally!" },
                    new EightBallResponse() { Text = "Of course!" },
                    new EightBallResponse() { Text = "As I see it, yes" },
                    new EightBallResponse() { Text = "My sources say yes" },
                    new EightBallResponse() { Text = "Yes" },
                    new EightBallResponse() { Text = "Most likely" },
                    new EightBallResponse() { Text = "Perhaps" },
                    new EightBallResponse() { Text = "Maybe" },
                    new EightBallResponse() { Text = "Not sure" },
                    new EightBallResponse() { Text = "It is uncertain" },
                    new EightBallResponse() { Text = "Ask me again later" },
                    new EightBallResponse() { Text = "Don't count on it" },
                    new EightBallResponse() { Text = "Probably not" },
                    new EightBallResponse() { Text = "Very doubtful" },
                    new EightBallResponse() { Text = "Most likely no" },
                    new EightBallResponse() { Text = "Nope" },
                    new EightBallResponse() { Text = "No" },
                    new EightBallResponse() { Text = "My sources say no" },
                    new EightBallResponse() { Text = "Dont even think about it" },
                    new EightBallResponse() { Text = "Definitely no" },
                    new EightBallResponse() { Text = "NO - It may cause disease contraction" }
                });

                BotConfig.Add(bc);

                this.SaveChanges();
            }
        }

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

            #region GuildConfig

            var configEntity = modelBuilder.Entity<GuildConfig>();
            configEntity
                .HasIndex(c => c.GuildId)
                .IsUnique();

            #endregion

            #region BotConfig
            var botConfigEntity = modelBuilder.Entity<BotConfig>();
            //botConfigEntity
            //    .HasMany(c => c.ModulePrefixes)
            //    .WithOne(mp => mp.BotConfig)
            //    .HasForeignKey(mp => mp.BotConfigId);
                
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

            #region Permission
            var permissionEntity = modelBuilder.Entity<Permission>();
            permissionEntity
                .HasOne(p => p.Next)
                .WithOne(p => p.Previous)
                .IsRequired(false);
            #endregion

            #region LogSettings

            //var logSettingEntity = modelBuilder.Entity<LogSetting>();

            //logSettingEntity
            //    .HasMany(ls => ls.IgnoredChannels)
            //    .WithOne(ls => ls.LogSetting)
            //    .HasPrincipalKey(ls => ls.id;

            //logSettingEntity
            //    .HasMany(ls => ls.IgnoredVoicePresenceChannelIds)
            //    .WithOne(ls => ls.LogSetting);
            #endregion

            #region MusicPlaylists
            var musicPlaylistEntity = modelBuilder.Entity<MusicPlaylist>();

            musicPlaylistEntity
                .HasMany(p => p.Songs)
                .WithOne()
                .OnDelete(Microsoft.EntityFrameworkCore.Metadata.DeleteBehavior.Cascade);


            #endregion

            #region PokeGame
            var pokeGameEntity = modelBuilder.Entity<UserPokeTypes>();

            pokeGameEntity
                .HasIndex(pt => pt.UserId)
                .IsUnique();


            #endregion
        }
    }
}
