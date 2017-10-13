using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Core.Services.Database;
using System;
using System.IO;
using System.Linq;

namespace NadekoBot.Core.Services
{
    public class DbService
    {
        private readonly DbContextOptions<NadekoContext> options;
        private readonly DbContextOptions<NadekoContext> migrateOptions;

        public DbService(IBotCredentials creds)
        {
            var builder = new SqliteConnectionStringBuilder(creds.Db.ConnectionString);
            builder.DataSource = Path.Combine(AppContext.BaseDirectory, builder.DataSource);
            
            var optionsBuilder = new DbContextOptionsBuilder<NadekoContext>();
            optionsBuilder.UseSqlite(builder.ToString());
            options = optionsBuilder.Options;

            optionsBuilder = new DbContextOptionsBuilder<NadekoContext>();
            optionsBuilder.UseSqlite(builder.ToString(), x => x.SuppressForeignKeyEnforcement());
            migrateOptions = optionsBuilder.Options;
        }

        public NadekoContext GetDbContext()
        {
            var context = new NadekoContext(options);
            if (context.Database.GetPendingMigrations().Any())
            {
                var mContext = new NadekoContext(migrateOptions);
                mContext.Database.Migrate();
                mContext.SaveChanges();
                mContext.Dispose();
            }
            context.Database.SetCommandTimeout(60);
            context.EnsureSeedData();

            //set important sqlite stuffs
            var conn = context.Database.GetDbConnection();
            conn.Open();

            context.Database.ExecuteSqlCommand("PRAGMA journal_mode=WAL");
            using (var com = conn.CreateCommand())
            {
                com.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=OFF";
                com.ExecuteNonQuery();
            }

            return context;
        }

        public IUnitOfWork UnitOfWork =>
            new UnitOfWork(GetDbContext());
    }
}