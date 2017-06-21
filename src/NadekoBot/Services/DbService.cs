using Microsoft.EntityFrameworkCore;
using NadekoBot.Services.Database;

namespace NadekoBot.Services
{
    public class DbService
    {
        private readonly DbContextOptions options;

        private readonly string _connectionString;

        public DbService(IBotCredentials creds)
        {
            _connectionString = creds.Db.ConnectionString;
            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseSqlite(creds.Db.ConnectionString);
            options = optionsBuilder.Options;
            //switch (_creds.Db.Type.ToUpperInvariant())
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
            context.Database.SetCommandTimeout(60);
            context.Database.Migrate();
            context.EnsureSeedData();

            return context;
        }

        public IUnitOfWork UnitOfWork =>
            new UnitOfWork(GetDbContext());
    }
}