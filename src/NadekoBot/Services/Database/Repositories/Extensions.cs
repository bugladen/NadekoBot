using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Repositories
{
    //some guy from stack overflow wrotethis, thanks to him :D
    public static class Extensions
    {
        public static TEntity Find<TEntity>(this DbSet<TEntity> set, params object[] keyValues) where TEntity : DbEntity
        {
            var context = ((IInfrastructure<IServiceProvider>)set).GetService<DbContext>();

            var entityType = context.Model.FindEntityType(typeof(TEntity));
            var key = entityType.FindPrimaryKey();

            var entries = context.ChangeTracker.Entries<TEntity>();

            var i = 0;
            foreach (var property in key.Properties)
            {
                entries = entries.Where(e => e.Property(property.Name).CurrentValue == keyValues[i]);
                i++;
            }

            var entry = entries.FirstOrDefault();
            if (entry != null)
            {
                // Return the local object if it exists.
                return entry.Entity;
            }
            
            // set.Where(x => x.Id == keyValues[0]);
            var parameter = Expression.Parameter(typeof(TEntity), "x");
            var query = set.Where((Expression<Func<TEntity, bool>>)
                Expression.Lambda(
                    Expression.Equal(
                        Expression.Property(parameter, "Id"),
                        Expression.Constant(keyValues[0])),
                    parameter));

            // Look in the database
            return query.FirstOrDefault();
        }
    }
}
