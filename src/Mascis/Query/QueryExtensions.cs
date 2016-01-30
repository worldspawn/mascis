using System.Collections.Generic;

namespace Mascis.Query
{
    public static class QueryExtensions
    {
        public static IList<TEntity> Execute<TEntity>(this Query<TEntity> query)
            where TEntity : class
        {
            return query.Session.Execute(query);
        }
    }
}