using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using NadekoBot.Services.Database.Impl;

namespace NadekoBot.Migrations
{
    [DbContext(typeof(NadekoSqliteContext))]
    partial class NadekoSqliteContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.0.0-rtm-21431");

            modelBuilder.Entity("NadekoBot.Services.Database.Models.Quote", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Keyword");

                    b.Property<string>("Text");

                    b.Property<string>("UserName");

                    b.HasKey("Id");

                    b.ToTable("Quotes");
                });
        }
    }
}
