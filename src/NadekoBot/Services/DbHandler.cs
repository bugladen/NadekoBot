using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NadekoBot.Services.Database;

namespace NadekoBot.Services
{
    public class DbHandler
    {
        private readonly DbContextOptions options;

        private string connectionString { get; }

        static DbHandler() { }

        public DbHandler(IBotCredentials creds)
        {
            connectionString = creds.Db.ConnectionString;
            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseSqlite(creds.Db.ConnectionString);
            options = optionsBuilder.Options;
            //switch (NadekoBot.Credentials.Db.Type.ToUpperInvariant())
            //{
            //    case "SQLITE":
            //        dbType = typeof(NadekoSqliteContext);
            //        break;
            //    //case "SQLSERVER":
            //    //    dbType = typeof(NadekoSqlServerContext);
            //    //    break;
            //    default:
            //        break;

            //}
        }

        public NadekoContext GetDbContext()
        {
            var context = new NadekoContext(options);
            context.Database.Migrate();
            context.EnsureSeedData();

            return context;
        }

        public IUnitOfWork UnitOfWork =>
            new UnitOfWork(GetDbContext());
    }
}