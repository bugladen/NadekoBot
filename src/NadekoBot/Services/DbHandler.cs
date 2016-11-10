using NadekoBot.Services.Database;
using NadekoBot.Services.Database.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services
{
    public class DbHandler
    {
        private Type dbType;

        private static DbHandler _instance = null;
        public static DbHandler Instance = _instance ?? (_instance = new DbHandler());
        

        static DbHandler() { }

        private DbHandler() {
            dbType = typeof(NadekoSqliteContext);
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

        public NadekoContext GetDbContext() => 
            Activator.CreateInstance(dbType) as NadekoContext;

        public IUnitOfWork GetUnitOfWork() =>
            new UnitOfWork(GetDbContext());

        public static IUnitOfWork UnitOfWork() =>
            DbHandler.Instance.GetUnitOfWork();
    }
}
