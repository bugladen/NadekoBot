using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using NadekoBot.Services.Database.Impl;

namespace NadekoBot.Migrations
{
    [DbContext(typeof(NadekoSqliteContext))]
    [Migration("20160910180231_first")]
    partial class first
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.0.0-rtm-21431");

            modelBuilder.Entity("NadekoBot.Services.Database.Models.BlacklistItem", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("BotConfigId");

                    b.Property<ulong>("ItemId");

                    b.HasKey("Id");

                    b.HasIndex("BotConfigId");

                    b.ToTable("BlacklistItem");
                });

            modelBuilder.Entity("NadekoBot.Services.Database.Models.BotConfig", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<ulong>("BufferSize");

                    b.Property<float>("CurrencyGenerationChance");

                    b.Property<int>("CurrencyGenerationCooldown");

                    b.Property<string>("CurrencyName");

                    b.Property<string>("CurrencyPluralName");

                    b.Property<string>("CurrencySign");

                    b.Property<bool>("DontJoinServers");

                    b.Property<bool>("ForwardMessages");

                    b.Property<bool>("ForwardToAllOwners");

                    b.Property<string>("RemindMessageFormat");

                    b.Property<bool>("RotatingStatuses");

                    b.HasKey("Id");

                    b.ToTable("BotConfig");
                });

            modelBuilder.Entity("NadekoBot.Services.Database.Models.ClashCaller", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<bool>("BaseDestroyed");

                    b.Property<string>("CallUser");

                    b.Property<int>("ClashWarId");

                    b.Property<int>("Stars");

                    b.Property<DateTime>("TimeAdded");

                    b.HasKey("Id");

                    b.HasIndex("ClashWarId");

                    b.ToTable("ClashCallers");
                });

            modelBuilder.Entity("NadekoBot.Services.Database.Models.ClashWar", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<ulong>("ChannelId");

                    b.Property<string>("EnemyClan");

                    b.Property<ulong>("GuildId");

                    b.Property<int>("Size");

                    b.Property<DateTime>("StartedAt");

                    b.Property<int>("WarState");

                    b.HasKey("Id");

                    b.ToTable("ClashOfClans");
                });

            modelBuilder.Entity("NadekoBot.Services.Database.Models.ConvertUnit", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("InternalTrigger");

                    b.Property<decimal>("Modifier");

                    b.Property<string>("UnitType");

                    b.HasKey("Id");

                    b.ToTable("ConversionUnits");
                });

            modelBuilder.Entity("NadekoBot.Services.Database.Models.Currency", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long>("Amount");

                    b.Property<ulong>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("Currency");
                });

            modelBuilder.Entity("NadekoBot.Services.Database.Models.Donator", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Amount");

                    b.Property<string>("Name");

                    b.Property<ulong>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("Donators");
                });

            modelBuilder.Entity("NadekoBot.Services.Database.Models.EightBallResponse", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("BotConfigId");

                    b.Property<string>("Text");

                    b.HasKey("Id");

                    b.HasIndex("BotConfigId");

                    b.ToTable("EightBallResponses");
                });

            modelBuilder.Entity("NadekoBot.Services.Database.Models.FollowedStream", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<ulong>("ChannelId");

                    b.Property<int?>("GuildConfigId");

                    b.Property<ulong>("GuildId");

                    b.Property<bool>("LastStatus");

                    b.Property<int>("Type");

                    b.Property<string>("Username");

                    b.HasKey("Id");

                    b.HasIndex("GuildConfigId");

                    b.ToTable("FollowedStream");
                });

            modelBuilder.Entity("NadekoBot.Services.Database.Models.GuildConfig", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<ulong>("AutoAssignRoleId");

                    b.Property<bool>("AutoDeleteByeMessages");

                    b.Property<bool>("AutoDeleteGreetMessages");

                    b.Property<int>("AutoDeleteGreetMessagesTimer");

                    b.Property<bool>("AutoDeleteSelfAssignedRoleMessages");

                    b.Property<ulong>("ByeMessageChannelId");

                    b.Property<string>("ChannelByeMessageText");

                    b.Property<string>("ChannelGreetMessageText");

                    b.Property<float>("DefaultMusicVolume");

                    b.Property<bool>("DeleteMessageOnCommand");

                    b.Property<string>("DmGreetMessageText");

                    b.Property<bool>("ExclusiveSelfAssignedRoles");

                    b.Property<ulong?>("GenerateCurrencyChannelId");

                    b.Property<ulong>("GreetMessageChannelId");

                    b.Property<ulong>("GuildId");

                    b.Property<int?>("LogSettingId");

                    b.Property<bool>("SendChannelByeMessage");

                    b.Property<bool>("SendChannelGreetMessage");

                    b.Property<bool>("SendDmGreetMessage");

                    b.Property<bool>("VoicePlusTextEnabled");

                    b.HasKey("Id");

                    b.HasIndex("GuildId")
                        .IsUnique();

                    b.HasIndex("LogSettingId");

                    b.ToTable("GuildConfigs");
                });

            modelBuilder.Entity("NadekoBot.Services.Database.Models.IgnoredLogChannel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<ulong>("ChannelId");

                    b.Property<int?>("LogSettingId");

                    b.HasKey("Id");

                    b.HasIndex("LogSettingId");

                    b.ToTable("IgnoredLogChannels");
                });

            modelBuilder.Entity("NadekoBot.Services.Database.Models.IgnoredVoicePresenceChannel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<ulong>("ChannelId");

                    b.Property<int?>("LogSettingId");

                    b.HasKey("Id");

                    b.HasIndex("LogSettingId");

                    b.ToTable("IgnoredVoicePresenceCHannels");
                });

            modelBuilder.Entity("NadekoBot.Services.Database.Models.LogSetting", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<bool>("ChannelCreated");

                    b.Property<bool>("ChannelDestroyed");

                    b.Property<ulong>("ChannelId");

                    b.Property<bool>("ChannelUpdated");

                    b.Property<bool>("IsLogging");

                    b.Property<bool>("LogUserPresence");

                    b.Property<bool>("LogVoicePresence");

                    b.Property<bool>("MessageDeleted");

                    b.Property<bool>("MessageReceived");

                    b.Property<bool>("MessageUpdated");

                    b.Property<bool>("UserBanned");

                    b.Property<bool>("UserJoined");

                    b.Property<bool>("UserLeft");

                    b.Property<ulong>("UserPresenceChannelId");

                    b.Property<bool>("UserUnbanned");

                    b.Property<bool>("UserUpdated");

                    b.Property<ulong>("VoicePresenceChannelId");

                    b.HasKey("Id");

                    b.ToTable("LogSettings");
                });

            modelBuilder.Entity("NadekoBot.Services.Database.Models.ModulePrefix", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("BotConfigId");

                    b.Property<string>("ModuleName");

                    b.Property<string>("Prefix");

                    b.HasKey("Id");

                    b.HasIndex("BotConfigId");

                    b.ToTable("ModulePrefixes");
                });

            modelBuilder.Entity("NadekoBot.Services.Database.Models.PlayingStatus", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("BotConfigId");

                    b.Property<string>("Status");

                    b.HasKey("Id");

                    b.HasIndex("BotConfigId");

                    b.ToTable("PlayingStatus");
                });

            modelBuilder.Entity("NadekoBot.Services.Database.Models.Quote", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<ulong>("AuthorId");

                    b.Property<string>("AuthorName")
                        .IsRequired();

                    b.Property<ulong>("GuildId");

                    b.Property<string>("Keyword")
                        .IsRequired();

                    b.Property<string>("Text")
                        .IsRequired();

                    b.HasKey("Id");

                    b.ToTable("Quotes");
                });

            modelBuilder.Entity("NadekoBot.Services.Database.Models.RaceAnimal", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("BotConfigId");

                    b.Property<string>("Icon");

                    b.Property<string>("Name");

                    b.HasKey("Id");

                    b.HasIndex("BotConfigId");

                    b.ToTable("RaceAnimals");
                });

            modelBuilder.Entity("NadekoBot.Services.Database.Models.Reminder", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<ulong>("ChannelId");

                    b.Property<bool>("IsPrivate");

                    b.Property<string>("Message");

                    b.Property<ulong>("ServerId");

                    b.Property<ulong>("UserId");

                    b.Property<DateTime>("When");

                    b.HasKey("Id");

                    b.ToTable("Reminders");
                });

            modelBuilder.Entity("NadekoBot.Services.Database.Models.Repeater", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<ulong>("ChannelId");

                    b.Property<ulong>("GuildId");

                    b.Property<TimeSpan>("Interval");

                    b.Property<string>("Message");

                    b.HasKey("Id");

                    b.HasIndex("ChannelId")
                        .IsUnique();

                    b.ToTable("Repeaters");
                });

            modelBuilder.Entity("NadekoBot.Services.Database.Models.SelfAssignedRole", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<ulong>("GuildId");

                    b.Property<ulong>("RoleId");

                    b.HasKey("Id");

                    b.HasIndex("GuildId", "RoleId")
                        .IsUnique();

                    b.ToTable("SelfAssignableRoles");
                });

            modelBuilder.Entity("NadekoBot.Services.Database.Models.TypingArticle", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Author");

                    b.Property<string>("Text");

                    b.HasKey("Id");

                    b.ToTable("TypingArticles");
                });

            modelBuilder.Entity("NadekoBot.Services.Database.Models.BlacklistItem", b =>
                {
                    b.HasOne("NadekoBot.Services.Database.Models.BotConfig")
                        .WithMany("Blacklist")
                        .HasForeignKey("BotConfigId");
                });

            modelBuilder.Entity("NadekoBot.Services.Database.Models.ClashCaller", b =>
                {
                    b.HasOne("NadekoBot.Services.Database.Models.ClashWar", "ClashWar")
                        .WithMany("Bases")
                        .HasForeignKey("ClashWarId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NadekoBot.Services.Database.Models.EightBallResponse", b =>
                {
                    b.HasOne("NadekoBot.Services.Database.Models.BotConfig")
                        .WithMany("EightBallResponses")
                        .HasForeignKey("BotConfigId");
                });

            modelBuilder.Entity("NadekoBot.Services.Database.Models.FollowedStream", b =>
                {
                    b.HasOne("NadekoBot.Services.Database.Models.GuildConfig")
                        .WithMany("FollowedStreams")
                        .HasForeignKey("GuildConfigId");
                });

            modelBuilder.Entity("NadekoBot.Services.Database.Models.GuildConfig", b =>
                {
                    b.HasOne("NadekoBot.Services.Database.Models.LogSetting", "LogSetting")
                        .WithMany()
                        .HasForeignKey("LogSettingId");
                });

            modelBuilder.Entity("NadekoBot.Services.Database.Models.IgnoredLogChannel", b =>
                {
                    b.HasOne("NadekoBot.Services.Database.Models.LogSetting", "LogSetting")
                        .WithMany("IgnoredChannels")
                        .HasForeignKey("LogSettingId");
                });

            modelBuilder.Entity("NadekoBot.Services.Database.Models.IgnoredVoicePresenceChannel", b =>
                {
                    b.HasOne("NadekoBot.Services.Database.Models.LogSetting", "LogSetting")
                        .WithMany("IgnoredVoicePresenceChannelIds")
                        .HasForeignKey("LogSettingId");
                });

            modelBuilder.Entity("NadekoBot.Services.Database.Models.ModulePrefix", b =>
                {
                    b.HasOne("NadekoBot.Services.Database.Models.BotConfig", "BotConfig")
                        .WithMany("ModulePrefixes")
                        .HasForeignKey("BotConfigId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NadekoBot.Services.Database.Models.PlayingStatus", b =>
                {
                    b.HasOne("NadekoBot.Services.Database.Models.BotConfig")
                        .WithMany("RotatingStatusMessages")
                        .HasForeignKey("BotConfigId");
                });

            modelBuilder.Entity("NadekoBot.Services.Database.Models.RaceAnimal", b =>
                {
                    b.HasOne("NadekoBot.Services.Database.Models.BotConfig")
                        .WithMany("RaceAnimals")
                        .HasForeignKey("BotConfigId");
                });
        }
    }
}
